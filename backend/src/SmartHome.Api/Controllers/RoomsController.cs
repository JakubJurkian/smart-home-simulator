using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(IRoomService roomService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetMyRooms()
    {
        var userId = GetCurrentUserId();
        return Ok(roomService.GetUserRooms(userId));
    }

    [HttpPost]
    public IActionResult AddRoom([FromBody] CreateRoomRequest request)
    {
        var userId = GetCurrentUserId();
        roomService.AddRoom(userId, request.Name);
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteRoom(Guid id)
    {
        try
        {
            roomService.DeleteRoom(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    [HttpPut("{id}")]
    public IActionResult RenameRoom(Guid id, [FromBody] string newName)
    {
        try
        {
            roomService.RenameRoom(id, newName);
            return Ok(new { message = "Room renamed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}