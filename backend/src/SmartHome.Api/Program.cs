using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using SmartHome.Infrastructure.Repositories;

using Serilog;
using SmartHome.Infrastructure.Services;

using SmartHome.Api.BackgroundServices;

using SmartHome.Api.Hubs;
using SmartHome.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog conf.
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        // download logs level settings (warning/info) from appsettings.json
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
});

// SERVICES SECTION (DI Container)
// register dependencies and tools

// Add support for Controllers (this enables DevicesController)
builder.Services.AddControllers();

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Konfiguracja Bazy Danych
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("SmartHome.Infrastructure")));
// line necessary for migration

builder.Services.AddSignalR();
builder.Services.AddScoped<IDeviceNotifier, SignalRNotifier>();

// REPOSITORY REGISTRATION
// AddSingleton because we store data in memory (RAM)
// for a database, we use AddScoped
// Singleton ensures data persists across different requests
// builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();

// We changed AddSingleton to AddScoped. DB lives shortly (for request)
builder.Services.AddScoped<IDeviceRepository, SqlDeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // React (localhost:5173) now can connect
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<TcpSmartHomeServer>();

var app = builder.Build();

// PIPELINE SECTION (Middleware)
// Here we define the request handling pipeline

app.UseCors("AllowReactApp");
// logs all HTTP requests.
app.UseSerilogRequestLogging();

// Enable Swagger UI only in Dev env.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Generates the interactive HTML page
}

app.UseHttpsRedirection(); // Redirect HTTP to HTTPS automatically
app.UseAuthorization();
app.MapControllers(); // Map endpoints from [ApiController] classes

app.MapHub<SmartHomeHub>("/hubs/smarthome"); //endpoint for WebSockets

app.Run(); // Start the app