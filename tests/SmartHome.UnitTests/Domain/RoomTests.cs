using FluentAssertions;
using SmartHome.Domain.Entities;

namespace SmartHome.UnitTests.Domain;

public class RoomTests
{
    [Fact]
    public void Rename_ShouldUpdateProperty_WhenNameIsValid()
    {
        // Arrange
        var room = new Room { Name = "Old Name" };
        string newName = "New Name";

        // Act
        room.Rename(newName);

        // Assert
        room.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rename_ShouldThrowException_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var room = new Room { Name = "Old Name" };

        // Act
        Action action = () => room.Rename(invalidName);

        // Assert
        action.Should().Throw<ArgumentException>()
              .WithMessage("Room name cannot be empty.");
    }

    [Fact]
    public void Properties_ShouldStoreValues()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var room = new Room
        {
            Id = id,
            UserId = userId,
            Name = "Test"
        };

        room.Id.Should().Be(id);
        room.UserId.Should().Be(userId);
    }
}