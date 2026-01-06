namespace SmartHome.Domain.Entities;
using SmartHome.Domain.Entities;

public class LightBulb(string name, string room) : Device(name, room, "LightBulb")
{
    public bool IsOn { get; set; } = false;
}