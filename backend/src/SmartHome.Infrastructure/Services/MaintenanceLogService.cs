using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Infrastructure.Services;

public class MaintenanceLogService(IMaintenanceLogRepository logRepository) : IMaintenanceLogService
{
    public Guid AddLog(Guid deviceId, string title, string description)
    {
        var id = Guid.NewGuid();
        var log = new MaintenanceLog
        {
            Id = id,
            DeviceId = deviceId,
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        logRepository.Add(log);
        return id;
    }

    public IEnumerable<MaintenanceLog> GetLogsForDevice(Guid deviceId)
    {
        return logRepository.GetByDeviceId(deviceId);
    }

    public void UpdateLog(Guid id, string title, string description)
    {
        var log = logRepository.GetById(id);
        if (log == null)
        {
            throw new Exception("Log not found.");
        }

        log.Title = title;
        log.Description = description;

        logRepository.Update(log);
    }

    public void DeleteLog(Guid id)
    {
        var log = logRepository.GetById(id);
        if (log == null)
        {
            throw new Exception("Log not found.");
        }
        logRepository.Delete(log);
    }
}