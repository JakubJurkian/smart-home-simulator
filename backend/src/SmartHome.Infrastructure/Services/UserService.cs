using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;

namespace SmartHome.Infrastructure.Services;

public class UserService(IUserRepository userRepository, IDeviceRepository _deviceRepository) : IUserService
{
    public Guid Register(string username, string email, string password, string role = "User")
    {
        // Check if email already exists
        var existingUser = userRepository.GetByEmail(email);
        if (existingUser != null)
        {
            throw new Exception("Email is already taken.");
        }

        // Hash the password (NEVER store plain text!)
        // BCrypt automatically generates a "salt", so two identical passwords will have different hashes.
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create the user object
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash, // Store the hash, not the actual password
            Role = role
        };

        // Save to the database
        userRepository.Add(user);
        return user.Id;
    }

    public User? Login(string email, string password)
    {
        // 1. Find the user by email
        var user = userRepository.GetByEmail(email);
        if (user == null)
        {
            return null; // Email not found
        }

        // 2. Verify the password
        // Compare the provided password with the stored hash
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return null; // Invalid password
        }

        return user; // Success!
    }

    public User? GetById(Guid id)
    {
        return userRepository.GetById(id);
    }

    public IEnumerable<User> SearchUsers(string phrase)
    {
        return userRepository.Search(phrase);
    }

    public void UpdateUser(Guid id, string newUsername, string? newPassword)
    {
        var user = userRepository.GetById(id) ?? throw new Exception("User not found.");

        // Update Username
        user.Username = newUsername;

        // Update Password (ONLY if provided)
        if (!string.IsNullOrEmpty(newPassword))
        {
            // Use BCrypt or your hashing logic here (same as in Register method)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        // Save changes
        userRepository.Update(user);
    }

    public void DeleteUser(Guid id)
    {
        var user = userRepository.GetById(id) ?? throw new Exception("User not found.");

        // remove all devices belonging to this user (cleanup)
        _deviceRepository.DeleteAllByUserId(id);

        // delete the user
        userRepository.Delete(user);
    }
}