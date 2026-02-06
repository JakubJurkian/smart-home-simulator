namespace SmartHome.Domain.Interfaces.Devices;

public interface IDeviceNotifier
{
    Task NotifyDeviceChanged();
    Task PushTemperature(Guid deviceId, double temperature);
}