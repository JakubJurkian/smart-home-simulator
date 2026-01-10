using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Interfaces;
using SmartHome.Domain.Entities;
using SmartHome.Api.Dtos;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(IDeviceRepository repo, ILogger<DevicesController> logger) : ControllerBase
{
    private readonly IDeviceRepository _repo = repo;

    [HttpGet]
    public IActionResult GetDevices()
    {
        logger.LogInformation("Retrieving the list of all devices from the database...");
        return Ok(_repo.GetAll());
    }

    [HttpPost("lightbulb")] // api/devices/lightbulb
    public IActionResult AddLightBulb([FromBody] CreateLightBulbRequest request)
    {
        logger.LogInformation("Request to add a new LightBulb: '{Name}' in '{Room}'", request.Name, request.Room);

        // write data from DTO (form) to real entity
        var newBulb = new LightBulb(request.Name, request.Room);
        
        // Save to repo
        _repo.Add(newBulb);

        logger.LogInformation("Successfully created LightBulb with ID: {DeviceId}", newBulb.Id);

        // Return 201 code 'Created' (Standard REST API)
        return CreatedAtAction(nameof(GetDeviceById), new { id = newBulb.Id }, newBulb);
    }

    [HttpGet("{id}")] // api/devices/[guid]
    public IActionResult GetDeviceById(Guid id)
    {
        var device = _repo.GetById(id);

        if (device == null)
        {
            logger.LogWarning("GetDeviceById failed: Device with ID {DeviceId} was not found.", id);
            return NotFound(); // Return 404 code
        }

        // logger.LogInformation("Device found: {DeviceName} ({DeviceId})", device.Name, device.Id);
        
        return Ok(device); // Return 200 code + object
    }

    [HttpPost("{id}/turn-on")]
    public IActionResult TurnOn(Guid id)
    {
        var device = _repo.GetById(id);

        if (device == null)
        {
             logger.LogWarning("TurnOn failed: Device with ID {DeviceId} not found.", id);
             return NotFound();
        }

        if (device is LightBulb bulb)
        {
            bulb.TurnOn();

            // We save changes to db
            _repo.Update(bulb);
            
            logger.LogInformation("Turned ON the light: {DeviceName} ({DeviceId})", bulb.Name, bulb.Id);
            return Ok(new { message = "Light turned on", isOn = bulb.IsOn });
        }
        
        logger.LogWarning("TurnOn failed: Device {DeviceId} is not a LightBulb!", id);
        return BadRequest("Device is not a light bulb.");
    }

    [HttpPost("{id}/turn-off")]
    public IActionResult TurnOff(Guid id)
    {
        var device = _repo.GetById(id);

        if (device == null)
        {
             logger.LogWarning("TurnOff failed: Device with ID {DeviceId} not found.", id);
             return NotFound();
        }

        if (device is LightBulb bulb)
        {
            bulb.TurnOff();

            _repo.Update(bulb);

            logger.LogInformation("Turned OFF the light: {DeviceName} ({DeviceId})", bulb.Name, bulb.Id);
            return Ok(new { message = "Light turned off", isOn = bulb.IsOn });
        }
        
        logger.LogWarning("TurnOff failed: Device {DeviceId} is not a LightBulb!", id);
        return BadRequest("Device is not a light bulb.");
    }

    [HttpPost("sensor")]
    public IActionResult AddSensor([FromBody] CreateSensorRequest request)
    {
        logger.LogInformation("Request to add a new Sensor: '{Name}' in '{Room}'", request.Name, request.Room);

        var newSensor = new TemperatureSensor(request.Name, request.Room);
        _repo.Add(newSensor);

        logger.LogInformation("Successfully created Sensor with ID: {DeviceId}", newSensor.Id);

        return CreatedAtAction(nameof(GetDeviceById), new { id = newSensor.Id }, newSensor);
    }

    [HttpGet("{id}/temperature")]
    public IActionResult GetTemperature(Guid id)
    {
        var device = _repo.GetById(id);
        
        if (device == null)
        {
            logger.LogWarning("GetTemperature failed: Device with ID {DeviceId} not found.", id);
            return NotFound();
        }

        if (device is TemperatureSensor sensor)
        {
            double currentTemp = sensor.GetReading();
            
            logger.LogInformation("Read temperature for '{DeviceName}': {Temp} C", sensor.Name, currentTemp);
            
            return Ok(new { temperature = currentTemp, unit = "Celsius" });
        }
        
        logger.LogWarning("GetTemperature failed: Device {DeviceId} does not support temperature readings.", id);
        return BadRequest("This device does not support temperature readings.");
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var device = _repo.GetById(id);
        
        if (device == null)
        {
            logger.LogWarning("Delete failed: Device with ID {DeviceId} not found.", id);
            return NotFound();
        }

        _repo.Delete(id);
        
        logger.LogInformation("Successfully deleted device with ID: {DeviceId}", id);

        // 204 No Content
        return NoContent();
    }
}