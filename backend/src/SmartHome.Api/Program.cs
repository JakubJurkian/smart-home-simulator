using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// SERVICE CONTAINER (Dependency Injection)

// Add support for Controllers (this activates DevicesController)
builder.Services.AddControllers();

// Register Swagger/OpenAPI generator
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the Device Repository (In-Memory Database)
// Singleton = Creates one shared instance for the entire application lifetime
builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();

var app = builder.Build();

// HTTP REQUEST PIPELINE (Middleware)

// Swagger UI (only in Development mode()
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Map Controllers (Routes incoming requests to the correct Controller actions)
app.MapControllers();

// Simple health check endpoint
app.MapGet("/", () => "Hello World! Smart Home API is running.");

app.Run();