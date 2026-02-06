namespace SmartHome.Domain.Interfaces.Rooms;

using SmartHome.Domain.Entities;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetAllAsync(Guid userId);
    Task<Guid> AddRoomAsync(string name, Guid userId);
    Task<bool> RenameRoomAsync(Guid id, string newName, Guid userId);
    Task<bool> DeleteRoomAsync(Guid id, Guid userId);
}