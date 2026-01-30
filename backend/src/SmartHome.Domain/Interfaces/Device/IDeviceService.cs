namespace SmartHome.Domain.Interfaces.Device;

using SmartHome.Domain.Entities;

public interface IDeviceService
{
    Task<IEnumerable<Device>> GetAllDevicesAsync(Guid userId, string? search = null);
    Task<Device?> GetDeviceByIdAsync(Guid id, Guid userId);

    Task<double?> GetTemperatureAsync(Guid id, Guid userId);

    Task<Guid> AddLightBulbAsync(string name, Guid roomId, Guid userId);
    Task<Guid> AddTemperatureSensorAsync(string name, Guid roomId, Guid userId);

    Task<bool> TurnOnAsync(Guid id, Guid userId);
    Task<bool> TurnOffAsync(Guid id, Guid userId);

    Task<bool> DeleteDeviceAsync(Guid id, Guid userId);
    Task<bool> UpdateTemperatureAsync(Guid id, double temp);

    Task<bool> RenameDeviceAsync(Guid id, string newName, Guid userId);
    Task<IEnumerable<Device>> GetAllServersSideAsync();
}