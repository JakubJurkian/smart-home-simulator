using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Infrastructure.Services;

public class DeviceService(IDeviceRepository repository) : IDeviceService
{
    public IEnumerable<Device> GetAllDevices(Guid userId)
    {
        return repository.GetAll(userId);
    }

    public Device? GetDeviceById(Guid id, Guid userId)
    {
        return repository.Get(id, userId);
    }

    public Guid AddLightBulb(string name, string room, Guid userId)
    {
        var bulb = new LightBulb(name, room) { UserId = userId };
        repository.Add(bulb);
        return bulb.Id;
    }

    public Guid AddTemperatureSensor(string name, string room, Guid userId)
    {
        var sensor = new TemperatureSensor(name, room) { UserId = userId };
        repository.Add(sensor);
        return sensor.Id;
    }

    public bool TurnOn(Guid id, Guid userId)
    {
        var device = repository.Get(id, userId);

        // Pattern Matching
        if (device is LightBulb bulb)
        {
            bulb.TurnOn();
            repository.Update(bulb);
            return true;
        }
        return false;
    }

    public bool TurnOff(Guid id, Guid userId)
    {
        var device = repository.Get(id, userId);

        if (device is LightBulb bulb)
        {
            bulb.TurnOff();
            repository.Update(bulb);
            return true;
        }
        return false;
    }

    public double? GetTemperature(Guid id, Guid userId)
    {
        var device = repository.Get(id, userId);
        if (device is TemperatureSensor sensor)
        {
            return sensor.GetReading();
        }
        return null;
    }

    public bool DeleteDevice(Guid id, Guid userId)
    {
        var device = repository.Get(id, userId);
        if (device == null) return false;

        repository.Delete(id);
        return true;
    }
}