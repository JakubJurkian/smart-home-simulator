using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHome.Api.Dtos;

public class CreateLogRequest
{
    [Required]
    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; set; }

    [Required]
    [StringLength(32, MinimumLength = 1)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateLogRequest
{
    [Required]
    [StringLength(32, MinimumLength = 1)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}