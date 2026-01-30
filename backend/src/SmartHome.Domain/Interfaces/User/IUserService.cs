namespace SmartHome.Domain.Interfaces.User;

using SmartHome.Domain.Entities;

public interface IUserService
{
    Task<Guid> RegisterAsync(string username, string email, string password);
    Task<User?> LoginAsync(string email, string password);

    Task<bool> UpdateUserAsync(Guid id, string username, string? newPassword);
    Task<bool> DeleteUserAsync(Guid id);
}

