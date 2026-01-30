using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Device;

namespace SmartHome.Infrastructure.Services;

public class DeviceService(IDeviceRepository repository, IDeviceNotifier notifier) : IDeviceService
{
    public async Task<IEnumerable<Device>> GetAllDevicesAsync(Guid userId, string? search = null)
    {
        return await repository.GetAllByUserIdAsync(userId, search);
    }

    public async Task<Device?> GetDeviceByIdAsync(Guid id, Guid userId)
    {
        var device = await repository.GetAsync(id, userId);
        return device;
    }

    public async Task<Guid> AddLightBulbAsync(string name, Guid roomId, Guid userId)
    {
        var bulb = new LightBulb(name, roomId) { UserId = userId };
        await repository.AddAsync(bulb);
        _ = notifier.NotifyDeviceChanged();
        return bulb.Id;
    }

    public async Task<Guid> AddTemperatureSensorAsync(string name, Guid roomId, Guid userId)
    {
        var sensor = new TemperatureSensor(name, roomId) { UserId = userId };
        await repository.AddAsync(sensor);
        _ = notifier.NotifyDeviceChanged();
        return sensor.Id;
    }

    public async Task<bool> TurnOnAsync(Guid id, Guid userId)
    {
        var device = await repository.GetAsync(id, userId);

        // Pattern Matching
        if (device is LightBulb bulb)
        {
            bulb.TurnOn();
            await repository.UpdateAsync(bulb);
            _ = notifier.NotifyDeviceChanged();
            return true;
        }
        return false;
    }

    public async Task<bool> TurnOffAsync(Guid id, Guid userId)
    {
        var device = await repository.GetAsync(id, userId);

        if (device is LightBulb bulb)
        {
            bulb.TurnOff();
            await repository.UpdateAsync(bulb);
            _ = notifier.NotifyDeviceChanged();
            return true;
        }
        return false;
    }

    public async Task<double?> GetTemperatureAsync(Guid id, Guid userId)
    {
        var device = await repository.GetAsync(id, userId);
        if (device is TemperatureSensor sensor)
        {
            return sensor.GetReading();
        }
        return null;
    }

    public async Task<bool> DeleteDeviceAsync(Guid id, Guid userId)
    {
        var device = await repository.GetAsync(id, userId);
        if (device == null) return false;

        await repository.DeleteAsync(device);
        _ = notifier.NotifyDeviceChanged();
        return true;
    }

    public async Task<bool> UpdateTemperatureAsync(Guid id, double temp)
    {
        var devices = await repository.GetAllAsync();
        var device = devices.FirstOrDefault(d => d.Id == id);

        if (device is TemperatureSensor sensor)
        {
            sensor.SetTemperature(temp);
            await repository.UpdateAsync(sensor);
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<IEnumerable<Device>> GetAllServersSideAsync()
    {
        // forward query to repo
        return await repository.GetAllAsync();
    }

    public async Task<bool> RenameDeviceAsync(Guid id, string newName, Guid userId)
    {
        var device = await repository.GetAsync(id, userId);
        if (device == null) return false;
        else
        {
            device.Rename(newName);
            await repository.UpdateAsync(device);
            await notifier.NotifyDeviceChanged();
            return true;
        }
    }
}