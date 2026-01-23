namespace SmartHome.Api.Dtos;

public record DeviceDto(
    Guid Id,
    string Name,
    Guid RoomId,
    string Type,
    bool? IsOn,
    double? CurrentTemperature
);