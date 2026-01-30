using SmartHome.Domain.Entities;
namespace SmartHome.Domain.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetAllAsync(Guid userId);
    Task<Guid> AddRoomAsync(string name, Guid userId);
    Task<bool> RenameRoomAsync(Guid id, string newName, Guid userId);
    Task<bool> DeleteRoomAsync(Guid id, Guid userId);
}