using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHome.Api.Dtos;

public class UpdateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [MinLength(8)]
    [MaxLength(128)]
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

public record UserDto(
    Guid Id,
    string Username,
    string Email
);