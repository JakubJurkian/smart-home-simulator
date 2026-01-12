namespace SmartHome.Domain.Interfaces;

public interface IDeviceNotifier
{
    Task NotifyDeviceChanged();
}