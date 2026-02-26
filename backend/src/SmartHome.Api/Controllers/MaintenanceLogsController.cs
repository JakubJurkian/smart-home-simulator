using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces.MaintenanceLogs;

namespace SmartHome.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/logs")]
public class MaintenanceLogsController(IMaintenanceLogService logService, ILogger<MaintenanceLogsController> logger) : ControllerBase
{
    // GET: api/logs/{deviceId}
    [HttpGet("{deviceId}")]
    public IActionResult GetLogs(Guid deviceId)
    {
        var logs = logService.GetLogsForDevice(deviceId);
        return Ok(logs);
    }

    // POST: api/logs
    [HttpPost]
    public IActionResult CreateLog([FromBody] CreateLogRequest request)
    {
        try
        {
            var newId = logService.AddLog(request.DeviceId, request.Title, request.Description);
            logger.LogInformation("Added new maintenance log for device {DeviceId}", request.DeviceId);
            return Ok(new { message = "Log added.", id = newId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/logs/{id}
    [HttpPut("{id}")]
    public IActionResult UpdateLog(Guid id, [FromBody] UpdateLogRequest request)
    {
        try
        {
            logService.UpdateLog(id, request.Title, request.Description);
            return Ok(new { message = "Log updated." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/logs/{id}
    [HttpDelete("{id}")]
    public IActionResult DeleteLog(Guid id)
    {
        try
        {
            logService.DeleteLog(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}