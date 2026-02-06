namespace SmartHome.Domain.Interfaces.MaintenanceLogs;
using SmartHome.Domain.Entities;

public interface IMaintenanceLogRepository
{
    void Add(MaintenanceLog log);
    IEnumerable<MaintenanceLog> GetByDeviceId(Guid deviceId);
    MaintenanceLog? GetById(Guid id);
    void Update(MaintenanceLog log);
    void Delete(MaintenanceLog log);
}