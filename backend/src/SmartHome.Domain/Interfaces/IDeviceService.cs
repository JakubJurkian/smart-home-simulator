using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IDeviceService
{
    IEnumerable<Device> GetAllDevices();
    Device? GetDeviceById(Guid id);
    
    double? GetTemperature(Guid id); 

    Guid AddLightBulb(string name, string room);
    Guid AddTemperatureSensor(string name, string room);

    bool TurnOn(Guid id);
    bool TurnOff(Guid id);

    bool DeleteDevice(Guid id);
}