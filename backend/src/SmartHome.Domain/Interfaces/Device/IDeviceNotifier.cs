namespace SmartHome.Domain.Interfaces.Device;

public interface IDeviceNotifier
{
    Task NotifyDeviceChanged();
    Task PushTemperature(Guid deviceId, double temperature);
}