using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

// Inject (DbContext) - connection with db
public class SqlDeviceRepository(SmartHomeDbContext context) : IDeviceRepository
{
    public void Add(Device device)
    {
        // Add to que
        context.Devices.Add(device);

        // We send SQL to db
        context.SaveChanges();
    }

    public IEnumerable<Device> GetAll(Guid userId)
    {
        // Download all to list
        return [.. context.Devices.Where(d => d.UserId == userId)];
    }

    public Device? Get(Guid id, Guid userId)
    {
        // Find by ID (null if not found)
        return context.Devices.FirstOrDefault(d => d.Id == id && d.UserId == userId);
    }

    public void Update(Device device)
    {
        context.Devices.Update(device);
        context.SaveChanges();
    }
    public void Delete(Guid id)
    {
        var device = context.Devices.Find(id);

        if (device != null)
        {
            context.Devices.Remove(device);

            context.SaveChanges();
        }
    }

    public void SetTemperature(Guid deviceId, double temperature)
    {

    }

    public IEnumerable<Device> GetAllServersSide()
    {
        return [.. context.Devices];
    }
}