using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Users;
using SmartHome.Infrastructure.Services;
using Xunit;

namespace SmartHome.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();


        _userService = new UserService(_userRepoMock.Object);
    }

    // --- REGISTER ---

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        string email = "test@test.com";
        string password = "secret_password";

        _userRepoMock.Setup(r => r.IsEmailTakenAsync(email))
                     .ReturnsAsync(false);

        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                     .Returns(Task.CompletedTask);

        // Act
        var userId = await _userService.RegisterAsync("Jan", email, password);

        // Assert
        userId.Should().NotBeEmpty();

        // check if add user & password is not plain string
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == email &&
            u.Username == "Jan" &&
            u.PasswordHash != password // password has to be hashed
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenEmailExists()
    {
        // Arrange
        string email = "already_taken@test.com";
        
        _userRepoMock.Setup(r => r.IsEmailTakenAsync(email))
                     .ReturnsAsync(true);

        // Act
        Func<Task> action = async () => await _userService.RegisterAsync("John", email, "password");

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Email is already registered.");
            
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // --- LOGIN ---

    [Fact]
    public async Task LoginAsync_ShouldReturnUser_WhenCredentialsAreCorrect()
    {
        // Arrange
        string email = "john@test.com";
        string password = "Qwe123@";

        // real hash, because UserService uses BCrypt.Verify()
        string realHash = BCrypt.Net.BCrypt.HashPassword(password);

        var userInDb = new User { Email = email, PasswordHash = realHash };

        _userRepoMock.Setup(r => r.GetByEmailAsync(email))
                     .ReturnsAsync(userInDb);

        // Act
        var result = await _userService.LoginAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(userInDb);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsWrong()
    {
        // Arrange
        string email = "john@test.com";
        string realHash = BCrypt.Net.BCrypt.HashPassword("correctPassword");
        var userInDb = new User { Email = email, PasswordHash = realHash };

        _userRepoMock.Setup(r => r.GetByEmailAsync(email))
                     .ReturnsAsync(userInDb);

        // Act
        var result = await _userService.LoginAsync(email, "incorrectPassword");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                     .ReturnsAsync((User?)null);
                     
        var result = await _userService.LoginAsync("unknown@test.pl", "password");
        
        result.Should().BeNull();
    }

    // --- UPDATE ---

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUsername_AndKeepPassword_WhenPasswordIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldHash = "old_hash";
        var user = new User { Id = userId, Username = "OldUsername", PasswordHash = oldHash };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // ACT
        var result = await _userService.UpdateUserAsync(userId, "NewUsername", null);

        // Assert
        result.Should().BeTrue();
        user.Username.Should().Be("NewUsername");
        user.PasswordHash.Should().Be(oldHash); // Password should not change

        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdatePassword_WhenPasswordIsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, PasswordHash = "old_hash" };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateUserAsync(userId, "Nick", "NewPassword123");

        // Assert
        user.PasswordHash.Should().NotBe("old_hash");
        user.PasswordHash.Should().NotBe("NewPassword123"); // Should be hashed

        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateUserAsync(userId, "Nick", "Password123");

        // Assert
        result.Should().BeFalse();
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    // --- DELETE ---

    [Fact]
    public async Task DeleteUserAsync_ShouldDeleteUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.DeleteAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        
        _userRepoMock.Verify(u => u.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFalse_WhenUserNotFound()
    {

        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeFalse();

        _userRepoMock.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
    }
}