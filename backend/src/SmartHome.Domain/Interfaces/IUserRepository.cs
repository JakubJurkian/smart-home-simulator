using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);

    Task<User?> GetByEmailAsync(string email);
    Task<bool> IsEmailTakenAsync(string email);
}