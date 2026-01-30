namespace SmartHome.Domain.Interfaces.Device;

using SmartHome.Domain.Entities;

public interface IDeviceRepository
{
    Task<IEnumerable<Device>> GetAllByUserIdAsync(Guid userId, string? search = null);
    Task<Device?> GetAsync(Guid id, Guid userId);
    Task AddAsync(Device device);
    Task UpdateAsync(Device device);
    Task DeleteAsync(Device device);
    Task<IEnumerable<Device>> GetAllAsync();
    Task DeleteAllByRoomIdAsync(Guid roomId, Guid userId);
    Task DeleteAllByUserIdAsync(Guid userId);
}