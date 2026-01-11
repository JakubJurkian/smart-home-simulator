using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Interfaces;
using SmartHome.Domain.Entities;
using SmartHome.Api.Dtos;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    private readonly IUserService _userService = userService;

    // POST: api/users/register
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        try
        {
            var userId = _userService.Register(request.Username, request.Email, request.Password);

            logger.LogInformation("User registered successfully. New ID: {UserId}", userId);
            // return 201 Created & ID of a new user
            return CreatedAtAction(nameof(Register), new { id = userId });
        }
        catch (Exception ex)
        {
            logger.LogWarning("Registration failed for {Email}. Reason: {Message}", request.Email, ex.Message);
            // 400 Bad Request
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST: api/users/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login attempt for email: {Email}", request.Email);
        var user = _userService.Login(request.Email, request.Password);

        if (user == null)
        {
            logger.LogWarning("Login failed for {Email} (Invalid credentials)", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        logger.LogInformation("User logged in successfully: {UserId}", user.Id);
        // cookies
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // JavaScript nie może tego ukraść (XSS Protection)
            Secure = false,  // Na localhost false, na produkcji true (HTTPS)
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        // Zapisujemy ID użytkownika w ciastku "UserId"
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
        // Remove Cookie
        Response.Cookies.Delete("userId");
        return Ok(new { message = "Logged out" });
    }
}