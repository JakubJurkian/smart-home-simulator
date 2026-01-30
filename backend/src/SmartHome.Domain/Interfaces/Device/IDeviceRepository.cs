using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IDeviceRepository
{
    IEnumerable<Device> GetAll(Guid userId);
    Device? Get(Guid id, Guid userId);
    void Add(Device device);
    void Update(Device device);
    void Delete(Guid id);
    IEnumerable<Device> GetAllServersSide();
    void DeleteAllByRoomId(Guid roomId);
    void DeleteAllByUserId(Guid userId);
}