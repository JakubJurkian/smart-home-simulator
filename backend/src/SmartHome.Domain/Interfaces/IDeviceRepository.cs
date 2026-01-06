using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IDeviceRepository
{
    IEnumerable<Device> GetAll();
    Device? GetById(Guid id);
    void Add(Device device);
}