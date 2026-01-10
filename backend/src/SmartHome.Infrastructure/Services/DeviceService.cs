using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Infrastructure.Services;

public class DeviceService(IDeviceRepository repository) : IDeviceService
{
    public IEnumerable<Device> GetAllDevices()
    {
        return repository.GetAll();
    }

    public Device? GetDeviceById(Guid id)
    {
        return repository.GetById(id);
    }

    public Guid AddLightBulb(string name, string room)
    {
        var bulb = new LightBulb(name, room);
        repository.Add(bulb);
        return bulb.Id;
    }

    public Guid AddTemperatureSensor(string name, string room)
    {
        var sensor = new TemperatureSensor(name, room);
        repository.Add(sensor);
        return sensor.Id;
    }

    public bool TurnOn(Guid id)
    {
        var device = repository.GetById(id);
        
        // Pattern Matching
        if (device is LightBulb bulb)
        {
            bulb.TurnOn();
            repository.Update(bulb);
            return true;
        }
        return false;
    }

    public bool TurnOff(Guid id)
    {
        var device = repository.GetById(id);
        
        if (device is LightBulb bulb)
        {
            bulb.TurnOff();
            repository.Update(bulb);
            return true;
        }
        return false;
    }

    public double? GetTemperature(Guid id)
    {
        var device = repository.GetById(id);
        if (device is TemperatureSensor sensor)
        {
            return sensor.GetReading();
        }
        return null;
    }

    public bool DeleteDevice(Guid id)
    {
        var device = repository.GetById(id);
        if (device == null) return false;

        repository.Delete(id);
        return true;
    }
}