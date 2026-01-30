using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Interfaces.Device;
using SmartHome.Domain.Entities;
using SmartHome.Api.Dtos;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(IDeviceService service, ILogger<DevicesController> logger) : ControllerBase
{

    [HttpGet]
    public IActionResult GetDevices()
    {
        try
        {
            var userId = GetCurrentUserId();
            var devices = service.GetAllDevicesAsync(userId);
            logger.LogInformation("Retrieving the list of all devices from the database...");
            return Ok(devices);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

    }

    [HttpPost("lightbulb")] // api/devices/lightbulb
    public async Task<IActionResult> AddLightBulb([FromBody] CreateDeviceRequest request)
    {
        logger.LogInformation("Request to add a new LightBulb: '{Name}' in '{Room}'", request.Name, request.RoomId);


        try
        {
            var userId = GetCurrentUserId();
            var id = await service.AddLightBulbAsync(request.Name, request.RoomId, userId);
            logger.LogInformation("Successfully created LightBulb with ID: {DeviceId}", id);
            return CreatedAtAction(nameof(GetDevices), new { id });

        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpGet("{id}")] // api/devices/[guid]
    public async Task<IActionResult> GetDeviceById(Guid id)
    {
        var userId = GetCurrentUserId();
        var device = await service.GetDeviceByIdAsync(id, userId);
        if (device == null)
        {
            logger.LogWarning("Device {DeviceId} not found.", id);
            return NotFound();
        }

        // logger.LogInformation("Device found: {DeviceName} ({DeviceId})", device.Name, device.Id);

        return Ok(device); // Return 200 code + object
    }

    [HttpPut("{id}/turn-on")]
    public async Task<IActionResult> TurnOn(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = await service.TurnOnAsync(id, userId);

        if (success)
        {
            logger.LogInformation("Turned ON device: {DeviceId}", id);
            var device = service.GetDeviceByIdAsync(id, userId);
            return Ok(new { message = "Device turned on", device });
        }

        logger.LogWarning("Failed to turn on device {DeviceId}", id);
        return BadRequest("Could not turn on device (not found or not a light bulb).");
    }

    [HttpPut("{id}/turn-off")]
    public async Task<IActionResult> TurnOff(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = await service.TurnOffAsync(id, userId);

        if (success)
        {
            logger.LogInformation("Turned OFF device: {DeviceId}", id);
            var device = service.GetDeviceByIdAsync(id, userId);
            return Ok(new { message = "Device turned off", device });
        }

        logger.LogWarning("Failed to turn off device {DeviceId}", id);
        return BadRequest("Could not turn off device.");
    }

    [HttpPost("temperaturesensor")]
    public async Task<IActionResult> AddSensor([FromBody] CreateDeviceRequest request)
    {
        logger.LogInformation("Request to add Sensor: '{Name}'", request.Name);
        var userId = GetCurrentUserId();
        var id = await service.AddTemperatureSensorAsync(request.Name, request.RoomId, userId);

        logger.LogInformation("Created Sensor with ID: {DeviceId}", id);
        return CreatedAtAction(nameof(GetDeviceById), new { id }, new { id });
    }

    [HttpGet("{id}/temperature")]
    public async Task<IActionResult> GetTemperature(Guid id)
    {
        var userId = GetCurrentUserId();
        var temp = await service.GetTemperatureAsync(id, userId);

        if (temp.HasValue)
        {
            logger.LogInformation("Read temperature for {DeviceId}: {Temp}", id, temp);
            return Ok(new { temperature = temp, unit = "Celsius" });
        }

        logger.LogWarning("Failed to read temperature for {DeviceId}", id);
        return BadRequest("Device not found or does not support temperature.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = await service.DeleteDeviceAsync(id, userId);

        if (!success)
        {
            logger.LogWarning("Delete failed: Device {DeviceId} not found.", id);
            return NotFound();
        }

        logger.LogInformation("Deleted device: {DeviceId}", id);
        return NoContent(); // 204 code
    }

    private Guid GetCurrentUserId()
    {
        if (Request.Cookies.TryGetValue("userId", out var userIdString) &&
            Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User not logged in");
    }

    [HttpGet("all-system")] // /api/devices/all-system
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAllSystemDevices()
    {
        // download all from server repository
        var devices = await service.GetAllServersSideAsync();

        var dtos = devices.Select(d => new DeviceDto(
        d.Id,
        d.Name,
        d.RoomId,
        d.GetType().Name,
        (d as LightBulb)?.IsOn,
        (d as TemperatureSensor)?.CurrentTemperature
    ));
        return Ok(dtos);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> RenameDevice(Guid id, [FromBody] RenameDeviceRequest request)
    {
        var userId = GetCurrentUserId();
        var success = await service.RenameDeviceAsync(id, request.Name, userId);

        if (success)
        {
            return Ok(new { message = "Device name changed." });
        }

        logger.LogWarning("Could not change device name.");
        return BadRequest("Could not change device name.");
    }
}