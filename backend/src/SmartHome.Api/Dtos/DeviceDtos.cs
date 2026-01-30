using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHome.Api.Dtos;

public class CreateDeviceRequest
{
    [Required]
    [StringLength(32, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 32 chars.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("roomId")]
    public Guid RoomId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class RenameDeviceRequest
{
    [Required]
    [StringLength(32, MinimumLength = 1)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public record DeviceDto(
    Guid Id,
    string Name,
    Guid RoomId,
    string Type,
    bool? IsOn,
    double? CurrentTemperature
);