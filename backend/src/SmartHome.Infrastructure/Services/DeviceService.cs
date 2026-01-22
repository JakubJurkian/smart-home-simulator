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

    public Guid AddDevice(string name, Guid roomId, string type, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Device name cannot be empty.");
        }

        Device newDevice;

        switch (type.ToLower())
        {
            case "lightbulb":
                newDevice = new LightBulb(name, roomId);
                break;
            case "temperaturesensor":
                newDevice = new TemperatureSensor(name, roomId);
                break;
            default:
                throw new ArgumentException($"Unknown device type: {type}");
        }

        repository.Add(newDevice);

        notifier.NotifyDeviceChanged();
        return newDevice.Id;
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
}