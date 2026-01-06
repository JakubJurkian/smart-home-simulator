using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Interfaces;

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
}