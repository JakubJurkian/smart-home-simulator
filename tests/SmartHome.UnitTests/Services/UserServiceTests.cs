using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Services;

namespace SmartHome.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _deviceRepoMock = new Mock<IDeviceRepository>();

        _userService = new UserService(_userRepoMock.Object, _deviceRepoMock.Object);
    }

    // register
    [Fact]
    public void Register_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        string email = "test@test.com";
        string password = "secret_password";

        _userRepoMock.Setup(r => r.GetByEmail(email)).Returns((User?)null);

        // Act
        var userId = _userService.Register("Jan", email, password);

        // Assert
        userId.Should().NotBeEmpty();

        // check if add user & password is not plain string
        _userRepoMock.Verify(r => r.Add(It.Is<User>(u =>
            u.Email == email &&
            u.Username == "Jan" &&
            u.PasswordHash != password // password has to be hashed
        )), Times.Once);
    }

    [Fact]
    public void Register_ShouldThrowException_WhenEmailExists()
    {
        // Arrange
        string email = "already_taken@test.com";
        _userRepoMock.Setup(r => r.GetByEmail(email)).Returns(new User());

        // Act
        Action action = () => _userService.Register("John", email, "password");

        // Assert
        action.Should().Throw<Exception>().WithMessage("Email is already taken.");
        _userRepoMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    // login
    [Fact]
    public void Login_ShouldReturnUser_WhenCredentialsAreCorrect()
    {
        // Arrange
        string email = "john@test.com";
        string password = "Qwe123@";

        // real hash, because UserService uses BCrypt.Verify()
        string realHash = BCrypt.Net.BCrypt.HashPassword(password);

        var userInDb = new User { Email = email, PasswordHash = realHash };

        _userRepoMock.Setup(r => r.GetByEmail(email)).Returns(userInDb);

        // Act
        var result = _userService.Login(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(userInDb);
    }

    [Fact]
    public void Login_ShouldReturnNull_WhenPasswordIsWrong()
    {
        // Arrange
        string email = "john@test.com";
        string realHash = BCrypt.Net.BCrypt.HashPassword("correctPassword");
        var userInDb = new User { Email = email, PasswordHash = realHash };

        _userRepoMock.Setup(r => r.GetByEmail(email)).Returns(userInDb);

        // Act - try to login with incorrect password
        var result = _userService.Login(email, "incorrectPassword");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Login_ShouldReturnNull_WhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByEmail(It.IsAny<string>())).Returns((User?)null);
        var result = _userService.Login("unknown@test.pl", "password");
        result.Should().BeNull();
    }

    // update user data
    [Fact]
    public void UpdateUser_ShouldUpdateUsername_AndKeepPassword_WhenPasswordIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldHash = "old_hash";
        var user = new User { Id = userId, Username = "OldUsername", PasswordHash = oldHash };

        _userRepoMock.Setup(r => r.GetById(userId)).Returns(user);

        // ACT - change only name
        _userService.UpdateUser(userId, "NewUsername", null);

        // Assert
        user.Username.Should().Be("NewUsername");
        user.PasswordHash.Should().Be(oldHash); // Password should not change

        _userRepoMock.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public void UpdateUser_ShouldUpdatePassword_WhenPasswordIsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, PasswordHash = "old_hash" };

        _userRepoMock.Setup(r => r.GetById(userId)).Returns(user);

        // Act
        _userService.UpdateUser(userId, "Nick", "NewPassword123");

        // Assert
        user.PasswordHash.Should().NotBe("old_hash"); // Password should change
        user.PasswordHash.Should().NotBe("NewPassword123"); // Password should be hashed

        _userRepoMock.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public void UpdateUser_ShouldThrowException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetById(userId)).Returns((User?)null);

        Action action = () => _userService.UpdateUser(userId, "Nick", "Password123");

        action.Should().Throw<Exception>().WithMessage("User not found.");
    }

    // delete
    [Fact]
    public void DeleteUser_ShouldDeleteDevicesAndUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };

        _userRepoMock.Setup(r => r.GetById(userId)).Returns(user);

        // Act
        _userService.DeleteUser(userId);

        // Assert
        // check if devices were deleted (Cascade Delete)
        _deviceRepoMock.Verify(d => d.DeleteAllByUserId(userId), Times.Once);

        _userRepoMock.Verify(u => u.Delete(user), Times.Once);
    }

    [Fact]
    public void DeleteUser_ShouldThrowException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetById(userId)).Returns((User?)null);

        Action action = () => _userService.DeleteUser(userId);

        action.Should().Throw<Exception>().WithMessage("User not found.");

        _deviceRepoMock.Verify(d => d.DeleteAllByUserId(It.IsAny<Guid>()), Times.Never);
        _userRepoMock.Verify(u => u.Delete(It.IsAny<User>()), Times.Never);
    }
}