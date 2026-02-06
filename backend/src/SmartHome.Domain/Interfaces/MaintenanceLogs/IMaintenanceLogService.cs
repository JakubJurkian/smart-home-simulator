namespace SmartHome.Domain.Interfaces.MaintenanceLogs;

using SmartHome.Domain.Entities;

public interface IMaintenanceLogService
{
    Guid AddLog(Guid deviceId, string title, string description);
    IEnumerable<MaintenanceLog> GetLogsForDevice(Guid deviceId);
    void UpdateLog(Guid id, string title, string description);
    void DeleteLog(Guid id);
}