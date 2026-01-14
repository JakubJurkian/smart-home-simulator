using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IDeviceService
{
    IEnumerable<Device> GetAllDevices(Guid userId);
    Device? GetDeviceById(Guid id, Guid userId);

    double? GetTemperature(Guid id, Guid userId);

    Guid AddLightBulb(string name, string room, Guid userId);
    Guid AddTemperatureSensor(string name, string room, Guid userId);

    bool TurnOn(Guid id, Guid userId);
    bool TurnOff(Guid id, Guid userId);

    bool DeleteDevice(Guid id, Guid userId);
    void UpdateTemperature(Guid id, double temp);

    IEnumerable<Device> GetAllServersSide();
}