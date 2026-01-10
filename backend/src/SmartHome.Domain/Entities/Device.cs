using System.Text.Json.Serialization;

namespace SmartHome.Domain.Entities;

[JsonDerivedType(typeof(LightBulb), typeDiscriminator: "LightBulb")]
[JsonDerivedType(typeof(TemperatureSensor), typeDiscriminator: "TemperatureSensor")]
public abstract class Device(string name, string room, string type)
{
    // Globally Unique Id
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = name;
    public string Room { get; private set; } = room;
    public string Type { get; protected set; } = type;
}