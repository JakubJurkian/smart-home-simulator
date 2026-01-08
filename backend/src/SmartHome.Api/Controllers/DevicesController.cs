using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Interfaces;
using SmartHome.Domain.Entities;
using SmartHome.Api.Dtos;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(IDeviceRepository repo) : ControllerBase
{
    private readonly IDeviceRepository _repo = repo;

    [HttpGet]
    public IActionResult GetDevices()
    {
        return Ok(_repo.GetAll());
    }
    [HttpPost("lightbulb")] // api/devices/lightbulb
    public IActionResult AddLightBulb([FromBody] CreateLightBulbRequest request)
    {
        // write data from DTO (form) to real entity
        var newBulb = new LightBulb(request.Name, request.Room);
        // Save to repo
        _repo.Add(newBulb);

        // Return 201 code 'Created' (Standard REST API)
        // return id of that created obj
        return CreatedAtAction(nameof(GetDeviceById), new { id = newBulb.Id }, newBulb);
    }

    [HttpGet("{id}")] // api/devices/[guid]
    public IActionResult GetDeviceById(Guid id)
    {
        var device = _repo.GetById(id);

        if (device == null)
        {
            return NotFound(); // Return 404 code
        }

        return Ok(device); // Return 200 code + object
    }
    [HttpGet("{id}/turn-on")]
    public IActionResult TurnOn(Guid id)
    {
        var device = _repo.GetById(id);
        if (device == null)
        {
            return NotFound();
        }
        // MAGIA C# (Pattern Matching):
        // Sprawdzamy: "Czy to urządzenie jest Żarówką?"
        // Jeśli tak, to od razu traktuj je jako zmienną 'bulb' typu LightBulb.
        if (device is LightBulb bulb)
        {
            bulb.IsOn = true;
            // Tutaj normalnie byłoby _repo.Update(bulb), ale w pamięci RAM referencja działa tak, 
            // że zmiana tutaj zmienia obiekt w liście repozytorium automatycznie.

            return Ok(bulb); // Zwracamy zaktualizowaną żarówkę
        }

        // Jeśli to nie żarówka (np. czujnik temperatury), nie możemy tego włączyć.
        return BadRequest("This device cannot be turned on.");
    }

    [HttpGet("{id}/turn-off")]
    public IActionResult TurnOff(Guid id)
    {
        var device = _repo.GetById(id);
        if (device == null)
        {
            return NotFound();
        }
        if (device is LightBulb bulb)
        {
            bulb.IsOn = false;
            return Ok(bulb);
        }
        return BadRequest("This device cannot be turned on.");
    }
    [HttpPost("sensor")]
    public IActionResult AddSensor([FromBody] CreateSensorRequest request)
    {
        var newSensor = new TemperatureSensor(request.Name, request.Room);
        _repo.Add(newSensor);
        return CreatedAtAction(nameof(GetDeviceById), new { id = newSensor.Id }, newSensor);
    }
    [HttpGet("{id}/temperature")]
    public IActionResult GetTemperature(Guid id)
    {
        var device = _repo.GetById(id);
        if (device == null)
        {
            return NotFound();
        }
        if (device is TemperatureSensor sensor)
        {
            double currentTemp = sensor.GetReading();
            return Ok(new { temperature = currentTemp, unit = "Celsius" });
        }
        return BadRequest("This device does not support temperature readings.");
    }
}