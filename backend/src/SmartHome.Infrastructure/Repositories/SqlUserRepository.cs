using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

public class SqlUserRepository(SmartHomeDbContext context) : IUserRepository
{
    public void Add(User user)
    {
        context.Users.Add(user);
        context.SaveChanges();
    }

    public User? GetByEmail(string email)
    {
        return context.Users.FirstOrDefault(u => u.Email == email);
    }

    public User? GetById(Guid id)
    {
        return context.Users.Find(id);
    }

    public IEnumerable<User> Search(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return context.Users.ToList();

        // We look by name or mail (WHERE ... LIKE ...)
        return context.Users
            .Where(u => u.Username.Contains(phrase) || u.Email.Contains(phrase))
            .ToList();
    }
}