using SmartHome.Domain.Interfaces.Devices;
using SmartHome.Domain.Interfaces.Users;
using SmartHome.Domain.Interfaces.Rooms;
using SmartHome.Domain.Interfaces.MaintenanceLogs;
using SmartHome.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SmartHome.Infrastructure.Repositories;
using Serilog;
using SmartHome.Infrastructure.Services;
using SmartHome.Api.BackgroundServices;
using SmartHome.Api.Hubs;
using SmartHome.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Logger Configuration ---
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
);

// --- 2. Services Configuration (DI Container) ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Database Provider Registration ---
if (builder.Environment.EnvironmentName != "Testing")
{
    if (builder.Environment.IsDevelopment())
    {
        // LOCAL: Use SQLite
        var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection")
                         ?? "Data Source=smarthome.db";
        builder.Services.AddDbContext<SmartHomeDbContext>(options =>
            options.UseSqlite(sqliteConn));

        Console.WriteLine("Registered SQLite for Development.");
    }
    else
    {
        // PRODUCTION (Azure): Use SQL Server
        var sqlServerConn = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<SmartHomeDbContext>(options =>
            options.UseSqlServer(sqlServerConn));

        Console.WriteLine("Registered SQL Server for Production.");
    }

    // Register Background Services for non-testing environments
    builder.Services.AddHostedService<TcpSmartHomeServer>();
    builder.Services.AddHostedService<MqttListenerService>();
}

// Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SmartHomeAuth";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

builder.Services.AddSignalR();
builder.Services.AddScoped<IDeviceNotifier, SignalRNotifier>();

// Repository and Service Registration
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();
builder.Services.AddScoped<IMaintenanceLogService, MaintenanceLogService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// --- 3. Database Initialization (Execute Migrations/Creation) ---
if (app.Environment.EnvironmentName != "Testing")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
        try
        {
            if (app.Environment.IsDevelopment())
            {
                // For SQLite locally, we just ensure the DB file and tables exist
                db.Database.EnsureCreated();
                Console.WriteLine("SQLite database is ready.");
            }
            else
            {
                // For SQL Server on Azure, we apply formal migrations
                db.Database.Migrate();
                Console.WriteLine("SQL Server migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization failed: {ex.Message}");
        }
    }
}

// --- 4. Pipeline Section (Middleware) ---
app.UseCors("AllowReactApp");
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SmartHomeHub>("/smarthomehub");

app.Run();