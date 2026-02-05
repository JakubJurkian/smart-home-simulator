using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Device;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

public class SqlDeviceRepository(SmartHomeDbContext context) : IDeviceRepository
{
    public async Task AddAsync(Device device)
    {
        context.Devices.Add(device);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Device>> GetAllByUserIdAsync(Guid userId, string? search = null)
    {
        var query = context.Devices
            .Include(d => d.Room)
            .Where(d => d.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));
        }

        return await query
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Device?> GetAsync(Guid id, Guid userId)
    {
        return await context.Devices
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    public async Task UpdateAsync(Device device)
    {
        context.Devices.Update(device);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Device device)
    {
        context.Devices.Remove(device);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Device>> GetAllAsync()
    {
        return await context.Devices
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        var userDevices = await context.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync();

        if (userDevices.Count != 0)
        {
            context.Devices.RemoveRange(userDevices);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllByRoomIdAsync(Guid roomId, Guid userId)
    {
        var devices = await context.Devices
            .Where(d => d.RoomId == roomId && d.UserId == userId)
            .ToListAsync();

        if (devices.Count != 0)
        {
            context.Devices.RemoveRange(devices);
            await context.SaveChangesAsync();
        }
    }
}