using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.User;

namespace SmartHome.Infrastructure.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<Guid> RegisterAsync(string username, string email, string password)
    {
        if (await userRepository.IsEmailTakenAsync(email))
        {
            throw new ArgumentException("Email is already registered.");
        }

        // password hashing -> BCrypt autom. generates salt so the same two password would have different hashes
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user);

        return user.Id;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        // password verification
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return null;
        }

        return user;
    }

    public async Task<bool> UpdateUserAsync(Guid id, string username, string? newPassword)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null) return false;

        if (user.Username != username)
        {
            user.Username = username;
        }

        if (!string.IsNullOrEmpty(newPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        await userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null) return false;

        await userRepository.DeleteAsync(user);
        return true;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id);
    }
}