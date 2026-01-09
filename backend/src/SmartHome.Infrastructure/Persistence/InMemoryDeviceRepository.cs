using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Infrastructure.Persistence;

public class InMemoryDeviceRepository : IDeviceRepository
{
    private static readonly List<Device> _devices =
    [
        new LightBulb("Kitchen Main", "Kitchen"),
        new LightBulb("Bedroom Lamp", "Bedroom"),
        new TemperatureSensor("Kitchen", "Kitchen"),
    ];

    public IEnumerable<Device> GetAll() => _devices;
    public Device? GetById(Guid id) => _devices.FirstOrDefault(d => d.Id == id);
    public void Add(Device device) => _devices.Add(device);
    public void Update(Device device)
    {

    }
    public void Delete(Guid id)
    {

    }
}