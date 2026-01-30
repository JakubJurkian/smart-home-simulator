using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(IRoomService roomService, ILogger<RoomsController> logger) : ControllerBase
{
    private Guid GetCurrentUserId()
    {
        if (Request.Cookies.TryGetValue("userId", out var userIdString) &&
            Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User not logged in.");
    }

    // GET: api/rooms
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var rooms = await roomService.GetAllAsync(userId);
        var roomDtos = rooms.Select(r => new RoomDto(r.Id, r.Name));
        return Ok(roomDtos);
    }

    // POST: api/rooms
    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var newId = await roomService.AddRoomAsync(request.Name, userId);

            logger.LogInformation("Room created: {RoomId}", newId);

            // 201 Created
            return CreatedAtAction(nameof(GetAll), new { id = newId });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            // 409 conflict
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating room");
            return StatusCode(500, new { message = "Internal Server Error" });
        }
    }

    // PUT: api/rooms/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> RenameRoom(Guid id, [FromBody] RenameRoomRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            bool updated = await roomService.RenameRoomAsync(id, request.Name, userId);

            if (!updated)
            {
                return NotFound(new { message = "Room not found." });
            }

            return Ok(new { message = "Room renamed." });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/rooms/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            bool deleted = await roomService.DeleteRoomAsync(id, userId);

            if (!deleted)
            {
                return NotFound(new { message = "Room not found." });
            }

            logger.LogInformation("Room deleted: {RoomId}", id);
            return NoContent(); // 204
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}