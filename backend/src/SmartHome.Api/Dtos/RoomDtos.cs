using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHome.Api.Dtos;

// DTO to return data to frontend (res)
public record RoomDto(
    Guid Id,
    string Name
);

public class CreateRoomRequest
{
    [Required(ErrorMessage = "Room name is required.")]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "Room name must be between 1 and 32 characters.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class RenameRoomRequest
{
    [Required(ErrorMessage = "New room name is required.")]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "Room name must be between 1 and 32 characters.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}