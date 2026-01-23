using System.Text.Json.Serialization;

namespace SmartHome.Domain.Entities;

[JsonDerivedType(typeof(LightBulb), typeDiscriminator: "LightBulb")]
[JsonDerivedType(typeof(TemperatureSensor), typeDiscriminator: "TemperatureSensor")]
public abstract class Device(string name, Guid roomId, string type)
{
    // Globally Unique Id
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; } = name;
    public Guid RoomId { get; set; } = roomId;// foreign key
    public Room? Room { get; set; }  // Navigation Property (for Entity Framework)
    public string Type { get; protected set; } = type;

    public Guid UserId { get; set; }
}