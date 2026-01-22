namespace SmartHome.Api.Dtos;
public record CreateDeviceRequest(string Name, Guid RoomId, string Type);