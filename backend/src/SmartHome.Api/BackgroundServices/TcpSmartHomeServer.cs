using System.Net;
using System.Net.Sockets;
using System.Text;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Api.BackgroundServices;

public class TcpSmartHomeServer : BackgroundService
{
    private readonly ILogger<TcpSmartHomeServer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int Port = 9000;

    public TcpSmartHomeServer(ILogger<TcpSmartHomeServer> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();

        _logger.LogInformation($"TCP Server started on port {Port}. Waiting for connections...");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait for client (e.g., Putty terminal)
            var client = await listener.AcceptTcpClientAsync(stoppingToken);

            // Handle client in background task so multiple clients can connect
            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Client connected!");
        
        // This variable stores the state of the current connection
        Guid? currentUserId = null;

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            await writer.WriteLineAsync("Welcome to SmartHome Raw TCP Interface!");
            await writer.WriteLineAsync("Please LOGIN first.");
            await writer.WriteLineAsync("Commands: LOGIN <email> <pass>, LIST, TOGGLE <GUID>, EXIT");

            while (client.Connected && !stoppingToken.IsCancellationRequested)
            {
                await writer.WriteAsync(currentUserId == null ? "> [Guest] " : "> [User] ");
                var commandLine = await reader.ReadLineAsync(stoppingToken);

                if (string.IsNullOrWhiteSpace(commandLine)) break;

                // Process command and pass the current session state (currentUserId)
                // We receive a tuple: (Response Message, New User ID if login happened)
                var (response, newUserId) = await ProcessCommand(commandLine, currentUserId);
                
                // Update session state if login was successful
                if (newUserId != null) currentUserId = newUserId;

                await writer.WriteLineAsync(response);

                if (commandLine.Trim().ToUpper() == "EXIT") break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TCP client");
        }
        finally
        {
            client.Close();
            _logger.LogInformation("🔌 Client disconnected.");
        }
    }

    // Returns a tuple: (Response String, LoggedInUserId?)
    private async Task<(string response, Guid? newUserId)> ProcessCommand(string commandInput, Guid? currentUserId)
    {
        var parts = commandInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return ("", null);

        var command = parts[0].ToUpper();

        // Scope Creation
        using var scope = _scopeFactory.CreateScope();
        var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        switch (command)
        {
            case "LOGIN":
                if (parts.Length < 3) return ("Usage: LOGIN <email> <password>", null);
                var email = parts[1];
                var pass = parts[2];
                
                var user = userService.Login(email, pass);
                if (user == null) return ("Invalid credentials.", null);

                return ($"Welcome {user.Username}! You are now logged in.", user.Id);

            case "LIST":
                if (currentUserId == null) return ("Access Denied. Please LOGIN first.", null);

                var devices = deviceService.GetAllDevices(currentUserId.Value);
                var sb = new StringBuilder();
                sb.AppendLine($"--- Devices for User {currentUserId} ---");
                foreach (var d in devices)
                {
                    // Assuming GetAllDevices returns DTOs or Entities
                    // Ideally we should use pattern matching on types or DTO properties
                    // Adjust properties based on your exact DTO/Entity definition
                    
                    string status = "[DEVICE]";
                    
                    // Simple logic to detect status based on dynamic check or DTO properties
                    if (d.Type.ToLower() == "lightbulb") 
                        status = (d as dynamic).IsOn == true ? "[ON] 💡" : "[OFF] 🌑";
                    
                    if (d.Type.ToLower().Contains("sensor")) 
                        status = $"[TEMP: {(d as dynamic).CurrentTemperature ?? "--"}°C] 🌡️";

                    sb.AppendLine($"{d.Id} | {d.Name} ({d.Room}) {status}");
                }
                if (sb.Length == 0) return ("No devices found.", null);
                return (sb.ToString(), null);

            case "TOGGLE":
                if (currentUserId == null) return ("Access Denied. Please LOGIN first.", null);
                if (parts.Length < 2) return ("Error: Provide ID", null);
                if (!Guid.TryParse(parts[1], out var id)) return ("Error: Invalid GUID", null);

                try 
                {
                    // Try to turn ON; if already ON, turn OFF (simple toggle logic)
                    // Note: Since service methods are void, we need to handle exceptions or check state first.
                    // For simplicity in TCP, we just try TurnOn, if it fails logic (e.g. is already on?), we might try TurnOff.
                    // But usually, user knows what they want. Let's assume we want to "Switch State".
                    // Since your Interface has TurnOn/TurnOff as void, we can't easily check state without Getting first.
                    
                    var device = deviceService.GetDeviceById(id, currentUserId.Value);
                    if (device == null) return ("Device not found.", null);

                    if (device is LightBulb bulb)
                    {
                        if (bulb.IsOn) 
                            deviceService.TurnOff(id, currentUserId.Value);
                        else 
                            deviceService.TurnOn(id, currentUserId.Value);
                        
                        return ("Device state toggled.", null);
                    }
                    return ("Device is not a lightbulb.", null);
                }
                catch (Exception ex)
                {
                    return ($"Error: {ex.Message}", null);
                }

            case "EXIT":
                return ("Goodbye.", null);

            default:
                return ("Unknown command.", null);
        }
    }
}