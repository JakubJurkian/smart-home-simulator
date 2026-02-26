using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces.Users;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SmartHome.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController(
    IUserService userService,
    ILogger<UsersController> logger) : ControllerBase
{
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User not logged in.");
    }

    // POST: api/users/register
    [HttpPost("register")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userService.LoginAsync(request.Email, request.Password);

        if (user == null)
        {
            logger.LogWarning("Login failed for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        logger.LogInformation("User logged in: {UserId}", user.Id);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email)
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

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
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("SmartHomeAuth");
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
                return StatusCode(403, new { message = "You are not allowed to modify this user." });
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
                return StatusCode(403, new { message = "You are not allowed to delete this user." });
            }

            bool deleted = await userService.DeleteUserAsync(id);

            if (!deleted)
            {
                return NotFound(new { message = "User not found." });
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("SmartHomeAuth");

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