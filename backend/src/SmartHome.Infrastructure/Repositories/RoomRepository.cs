using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

public class RoomRepository(SmartHomeDbContext context) : IRoomRepository
{
    public async Task AddAsync(Room room)
    {
        await context.Rooms.AddAsync(room); 
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Room>> GetAllByUserIdAsync(Guid userId)
    {
        return await context.Rooms
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Room?> GetByIdAsync(Guid id)
    {
        return await context.Rooms.FindAsync(id);
    }

    public async Task UpdateAsync(Room room)
    {
        context.Rooms.Update(room);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Room room)
    {
        context.Rooms.Remove(room);
        await context.SaveChangesAsync();
    }
    public async Task<bool> RoomNameExistsAsync(string name, Guid userId)
    {
        return await context.Rooms
            .AnyAsync(r => r.UserId == userId && r.Name.ToLower() == name.ToLower());
    }
}