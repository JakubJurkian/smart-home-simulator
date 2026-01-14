namespace SmartHome.Domain.Interfaces;

public interface IDeviceNotifier
{
    Task NotifyDeviceChanged();
    Task PushTemperature(Guid deviceId, double temperature);
}