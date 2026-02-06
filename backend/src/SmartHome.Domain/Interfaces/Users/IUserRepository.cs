namespace SmartHome.Domain.Interfaces.Users;

using SmartHome.Domain.Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);

    Task<User?> GetByEmailAsync(string email);
    Task<bool> IsEmailTakenAsync(string email);
}