namespace SmartHome.Domain.Interfaces.Room;

using SmartHome.Domain.Entities;

public interface IRoomRepository
{
    Task AddAsync(Room room);
    Task<IEnumerable<Room>> GetAllByUserIdAsync(Guid userId);
    Task<Room?> GetByIdAsync(Guid id);
    Task UpdateAsync(Room room);
    Task DeleteAsync(Room room);
    Task<bool> RoomNameExistsAsync(string name, Guid userId);
}