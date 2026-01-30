using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Room;

namespace SmartHome.Infrastructure.Services;

public class RoomService(IRoomRepository roomRepository) : IRoomService
{
    public async Task<IEnumerable<Room>> GetAllAsync(Guid userId)
    {
        return await roomRepository.GetAllByUserIdAsync(userId);
    }

    public async Task<Guid> AddRoomAsync(string name, Guid userId)
    {
        bool exists = await roomRepository.RoomNameExistsAsync(name, userId);
        if (exists)
        {
            throw new ArgumentException($"Room '{name}' already exists.");
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId
        };

        await roomRepository.AddAsync(room);

        return room.Id;
    }

    public async Task<bool> RenameRoomAsync(Guid id, string newName, Guid userId)
    {
        var room = await roomRepository.GetByIdAsync(id);

        if (room == null || room.UserId != userId)
        {
            return false;
        }

        if (room.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        bool nameTaken = await roomRepository.RoomNameExistsAsync(newName, userId);
        if (nameTaken)
        {
            throw new ArgumentException($"Room '{newName}' already exists.");
        }

        room.Name = newName;
        await roomRepository.UpdateAsync(room);

        return true;
    }

    public async Task<bool> DeleteRoomAsync(Guid id, Guid userId)
    {
        var room = await roomRepository.GetByIdAsync(id);

        if (room == null || room.UserId != userId)
        {
            return false;
        }

        await roomRepository.DeleteAsync(room);

        return true;
    }
}