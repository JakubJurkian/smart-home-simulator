using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Interfaces;
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
            var devices = service.GetAllDevices(userId);
            logger.LogInformation("Retrieving the list of all devices from the database...");
            return Ok(devices);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

    }

    [HttpPost] // api/devices
    public IActionResult AddDevice([FromBody] CreateDeviceRequest request)
    {
        logger.LogInformation("Request to add device: '{Name}' (Type: {Type}) in Room: {Room}",
            request.Name, request.Type, request.RoomId);


        try
        {
            var userId = GetCurrentUserId();
            var id = service.AddDevice(request.Name, request.RoomId, request.Type, userId);
            logger.LogInformation("Successfully created {Type} with ID: {DeviceId}", request.Type, id);
            return CreatedAtAction(nameof(GetDeviceById), new { id }, new { id });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Failed to create device: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("{id}")] // api/devices/[guid]
    public IActionResult GetDeviceById(Guid id)
    {
        var userId = GetCurrentUserId();
        var device = service.GetDeviceById(id, userId);
        if (device == null)
        {
            logger.LogWarning("Device {DeviceId} not found.", id);
            return NotFound();
        }

        // logger.LogInformation("Device found: {DeviceName} ({DeviceId})", device.Name, device.Id);

        return Ok(device); // Return 200 code + object
    }

    [HttpPut("{id}/turn-on")]
    public IActionResult TurnOn(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = service.TurnOn(id, userId);

        if (success)
        {
            logger.LogInformation("Turned ON device: {DeviceId}", id);
            var device = service.GetDeviceById(id, userId);
            return Ok(new { message = "Device turned on", device });
        }

        logger.LogWarning("Failed to turn on device {DeviceId}", id);
        return BadRequest("Could not turn on device (not found or not a light bulb).");
    }

    [HttpPut("{id}/turn-off")]
    public IActionResult TurnOff(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = service.TurnOff(id, userId);

        if (success)
        {
            logger.LogInformation("Turned OFF device: {DeviceId}", id);
            var device = service.GetDeviceById(id, userId);
            return Ok(new { message = "Device turned off", device });
        }

        logger.LogWarning("Failed to turn off device {DeviceId}", id);
        return BadRequest("Could not turn off device.");
    }

    [HttpGet("{id}/temperature")]
    public IActionResult GetTemperature(Guid id)
    {
        var userId = GetCurrentUserId();
        var temp = service.GetTemperature(id, userId);

        if (temp.HasValue)
        {
            logger.LogInformation("Read temperature for {DeviceId}: {Temp}", id, temp);
            return Ok(new { temperature = temp, unit = "Celsius" });
        }

        logger.LogWarning("Failed to read temperature for {DeviceId}", id);
        return BadRequest("Device not found or does not support temperature.");
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var success = service.DeleteDevice(id, userId);

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
    public ActionResult<IEnumerable<DeviceDto>> GetAllSystemDevices()
    {
        // download all from server repository
        var devices = service.GetAllServersSide();

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
}