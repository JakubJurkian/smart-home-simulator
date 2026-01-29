using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Infrastructure.Services;

public class DeviceService(IDeviceRepository repository, IDeviceNotifier notifier) : IDeviceService
{
    public IEnumerable<Device> GetAllDevices(Guid userId)
    {
        return repository.GetAll(userId);
    }

    public Device? GetDeviceById(Guid id, Guid userId)
    {
        return repository.Get(id, userId);
    }

    public Guid AddLightBulb(string name, Guid roomId, Guid userId)
    {
        var bulb = new LightBulb(name, roomId) { UserId = userId };
        repository.Add(bulb);
        _ = notifier.NotifyDeviceChanged();
        return bulb.Id;
    }

    public Guid AddTemperatureSensor(string name, Guid roomId, Guid userId)
    {
        var sensor = new TemperatureSensor(name, roomId) { UserId = userId };
        repository.Add(sensor);
        _ = notifier.NotifyDeviceChanged();
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
            _ = notifier.NotifyDeviceChanged();
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
            _ = notifier.NotifyDeviceChanged();
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
        _ = notifier.NotifyDeviceChanged();
        return true;
    }

    public void UpdateTemperature(Guid id, double temp)
    {
        var device = repository.GetAllServersSide().FirstOrDefault(d => d.Id == id);

        if (device is TemperatureSensor sensor)
        {
            sensor.SetTemperature(temp);
            repository.Update(sensor);
        }
    }

    public IEnumerable<Device> GetAllServersSide()
    {
        // Przekazujemy zapytanie do repozytorium
        return repository.GetAllServersSide();
    }

    public bool RenameDevice(Guid id, string newName, Guid userId)
    {
        var device = repository.Get(id, userId);
        if (device == null) return false;
        else
        {
            device.Rename(newName);
            repository.Update(device);
            notifier.NotifyDeviceChanged();
            return true;
        }
    }
}