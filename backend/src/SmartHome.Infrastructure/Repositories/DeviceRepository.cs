using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Device;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

// Inject (DbContext) - connection with db
public class DeviceRepository(SmartHomeDbContext context) : IDeviceRepository
{
    public async Task AddAsync(Device device)
    {
        // Add to que
        context.Devices.Add(device);

        // We send SQL to db
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Device>> GetAllByUserIdAsync(Guid userId)
    {
        // Download all to list
        return await context.Devices.Include(d => d.Room).Where(d => d.UserId == userId).AsNoTracking().ToListAsync();
    }

    public async Task<Device?> GetAsync(Guid id, Guid userId)
    {
        // Find by ID (null if not found)
        return await context.Devices.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
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
        return await context.Devices.AsNoTracking().ToListAsync();
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        await context.Devices.Where(d => d.UserId == userId).ExecuteDeleteAsync();
    }

    public async Task DeleteAllByRoomIdAsync(Guid roomId, Guid userId)
    {
        await context.Devices
            .Where(d => d.RoomId == roomId && d.UserId == userId)
            .ExecuteDeleteAsync();
    }
}