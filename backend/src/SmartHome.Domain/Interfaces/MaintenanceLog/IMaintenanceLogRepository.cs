using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IMaintenanceLogRepository
{
    void Add(MaintenanceLog log);
    IEnumerable<MaintenanceLog> GetByDeviceId(Guid deviceId);
    MaintenanceLog? GetById(Guid id);
    void Update(MaintenanceLog log);
    void Delete(MaintenanceLog log);
}