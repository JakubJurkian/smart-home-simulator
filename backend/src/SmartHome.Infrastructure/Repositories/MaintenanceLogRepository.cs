using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.MaintenanceLogs;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

public class MaintenanceLogRepository(SmartHomeDbContext context) : IMaintenanceLogRepository
{
    public void Add(MaintenanceLog log)
    {
        context.MaintenanceLogs.Add(log);
        context.SaveChanges();
    }

    public IEnumerable<MaintenanceLog> GetByDeviceId(Guid deviceId)
    {
        // Order by date descending (newest first)
        return context.MaintenanceLogs
            .Where(l => l.DeviceId == deviceId)
            .OrderByDescending(l => l.CreatedAt)
            .ToList();
    }

    public MaintenanceLog? GetById(Guid id)
    {
        return context.MaintenanceLogs.Find(id);
    }

    public void Update(MaintenanceLog log)
    {
        context.MaintenanceLogs.Update(log);
        context.SaveChanges();
    }

    public void Delete(MaintenanceLog log)
    {
        context.MaintenanceLogs.Remove(log);
        context.SaveChanges();
    }
}