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

var builder = WebApplication.CreateBuilder(args);

// Serilog conf.
builder.Host.UseSerilog((context, configuration) =>
    configuration
    // download logs level settings (warning/info) from appsettings.json
    .ReadFrom.Configuration(context.Configuration)
);

// SERVICES SECTION (DI Container)
// register dependencies and tools

// Add support for Controllers (this enables DevicesController)
builder.Services.AddControllers();

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
// services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
// This prevents provider conflicts with SQLite during Integration Tests
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddDbContext<SmartHomeDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddSignalR();
builder.Services.AddScoped<IDeviceNotifier, SignalRNotifier>();

// REPOSITORY REGISTRATION
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<TcpSmartHomeServer>();
    builder.Services.AddHostedService<MqttListenerService>();
}

var app = builder.Build();

// PIPELINE SECTION (Middleware)
// Here we define the request handling pipeline

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SmartHomeHub>("/smarthomehub");

app.Run();