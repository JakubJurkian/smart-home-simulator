using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IUserRepository
{
    void Add(User user);
    User? GetByEmail(string email);
    User? GetById(Guid id);
    IEnumerable<User> Search(string phrase); // Points for searching
}