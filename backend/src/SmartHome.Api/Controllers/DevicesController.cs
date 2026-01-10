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
        logger.LogInformation("Retrieving the list of all devices from the database...");
        return Ok(service.GetAllDevices());
    }

    [HttpPost("lightbulb")] // api/devices/lightbulb
    public IActionResult AddLightBulb([FromBody] CreateLightBulbRequest request)
    {
        logger.LogInformation("Request to add a new LightBulb: '{Name}' in '{Room}'", request.Name, request.Room);

        // write data from DTO (form) to real entity
        var id = service.AddLightBulb(request.Name, request.Room);

        logger.LogInformation("Successfully created LightBulb with ID: {DeviceId}", id);

        // Return 201 code 'Created' (Standard REST API)
        return CreatedAtAction(nameof(GetDeviceById), new { id }, new { id });
    }

    [HttpGet("{id}")] // api/devices/[guid]
    public IActionResult GetDeviceById(Guid id)
    {
        var device = service.GetDeviceById(id);
        if (device == null)
        {
            logger.LogWarning("Device {DeviceId} not found.", id);
            return NotFound();
        }

        // logger.LogInformation("Device found: {DeviceName} ({DeviceId})", device.Name, device.Id);

        return Ok(device); // Return 200 code + object
    }

    [HttpPost("{id}/turn-on")]
    public IActionResult TurnOn(Guid id)
    {
        var success = service.TurnOn(id);

        if (success)
        {
            logger.LogInformation("Turned ON device: {DeviceId}", id);
            var device = service.GetDeviceById(id);
            return Ok(new { message = "Device turned on", device });
        }

        logger.LogWarning("Failed to turn on device {DeviceId}", id);
        return BadRequest("Could not turn on device (not found or not a light bulb).");
    }

    [HttpPost("{id}/turn-off")]
    public IActionResult TurnOff(Guid id)
    {
        var success = service.TurnOff(id);

        if (success)
        {
            logger.LogInformation("Turned OFF device: {DeviceId}", id);
            var device = service.GetDeviceById(id);
            return Ok(new { message = "Device turned off", device });
        }

        logger.LogWarning("Failed to turn off device {DeviceId}", id);
        return BadRequest("Could not turn off device.");
    }

    [HttpPost("sensor")]
    public IActionResult AddSensor([FromBody] CreateSensorRequest request)
    {
        logger.LogInformation("Request to add Sensor: '{Name}'", request.Name);

        var id = service.AddTemperatureSensor(request.Name, request.Room);

        logger.LogInformation("Created Sensor with ID: {DeviceId}", id);
        return CreatedAtAction(nameof(GetDeviceById), new { id }, new { id });
    }

    [HttpGet("{id}/temperature")]
    public IActionResult GetTemperature(Guid id)
    {
        var temp = service.GetTemperature(id);

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
        var success = service.DeleteDevice(id);

        if (!success)
        {
            logger.LogWarning("Delete failed: Device {DeviceId} not found.", id);
            return NotFound();
        }

        logger.LogInformation("Deleted device: {DeviceId}", id);
        return NoContent(); // 204 code
    }
}