using SmartHome.Domain.Entities;

namespace SmartHome.Domain.Interfaces;

public interface IUserService
{
    // Register: return id of a new user (or exception if email exists)
    Guid Register(string username, string email, string password, string role = "User");

    // Check password & return the user (or null)
    User? Login(string email, string password);

    // Download user's data
    User? GetById(Guid id);

    // Search
    IEnumerable<User> SearchUsers(string phrase);

    void UpdateUser(Guid id, string newUsername, string? newPassword);

    void DeleteUser(Guid id);
}