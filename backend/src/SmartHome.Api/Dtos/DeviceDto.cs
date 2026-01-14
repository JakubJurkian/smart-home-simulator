namespace SmartHome.Api.Dtos;

public record DeviceDto(
    Guid Id,
    string Name,
    string Room,
    string Type, 
    bool? IsOn, 
    double? LastTemperature
);