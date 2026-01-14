using Microsoft.AspNetCore.SignalR;
using SmartHome.Api.Hubs;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Api.Services;

public class SignalRNotifier(IHubContext<SmartHomeHub> hubContext) : IDeviceNotifier
{
    private readonly IHubContext<SmartHomeHub> _hubContext = hubContext;

    public async Task NotifyDeviceChanged()
    {
        // Wysyłamy wiadomość "RefreshDevices" do WSZYSTKICH podłączonych klientów
        await _hubContext.Clients.All.SendAsync("RefreshDevices");
    }

    public async Task PushTemperature(Guid deviceId, double temperature)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveTemperature", deviceId, temperature);
    }
}