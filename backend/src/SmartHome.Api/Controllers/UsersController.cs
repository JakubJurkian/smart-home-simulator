using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces.Users;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(
    IUserService userService,
    ILogger<UsersController> logger,
    IHostEnvironment env) : ControllerBase
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

    // POST: api/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var userId = await userService.RegisterAsync(request.Username, request.Email, request.Password);

            logger.LogInformation("User registered successfully. ID: {UserId}", userId);

            return StatusCode(201, new { id = userId, message = "Registration successful" });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Registration failed: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registration error");
            return StatusCode(500, new { message = "Internal Server Error" });
        }
    }

    // POST: api/users/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userService.LoginAsync(request.Email, request.Password);

        if (user == null)
        {
            logger.LogWarning("Login failed for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        logger.LogInformation("User logged in: {UserId}", user.Id);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // JS nie ma dostępu do tego ciastka
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("userId", user.Id.ToString(), cookieOptions);

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            message = "Login successful!"
        });
    }

    // POST: api/users/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("userId");
        return Ok(new { message = "Logged out" });
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
            {
                return Forbid(); // 403 Forbidden
            }

            bool updated = await userService.UpdateUserAsync(id, request.Username, request.Password);

            if (!updated)
            {
                return NotFound(new { message = "User not found." });
            }

            logger.LogInformation("User {UserId} updated profile.", id);
            return Ok(new { message = "Profile updated successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(); // 401
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

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
            {
                return Forbid();
            }

            bool deleted = await userService.DeleteUserAsync(id);

            if (!deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            Response.Cookies.Delete("userId");

            logger.LogInformation("User account {UserId} deleted.", id);
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

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();

            var user = await userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Not logged in" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Internal Server Error" });
        }
    }
}