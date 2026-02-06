using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Rooms;
using SmartHome.Infrastructure.Services;

namespace SmartHome.UnitTests.Services;

public class RoomServiceTests
{
    // mocking dependencies
    private readonly Mock<IRoomRepository> _roomRepoMock;

    // system under test (sut)
    private readonly RoomService _roomService;

    public RoomServiceTests()
    {
        _roomRepoMock = new Mock<IRoomRepository>();

        _roomService = new RoomService(_roomRepoMock.Object);
    }

    [Fact]
    public async Task AddRoomAsync_ShouldCallRepository_WhenNameIsValid()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var roomName = "Kitchen";

        _roomRepoMock.Setup(r => r.RoomNameExistsAsync(roomName, userId))
                     .ReturnsAsync(false);

        _roomRepoMock.Setup(r => r.AddAsync(It.IsAny<Room>()))
                     .Returns(Task.CompletedTask);

        // ACT
        await _roomService.AddRoomAsync(roomName, userId);

        // ASSERT
        _roomRepoMock.Verify(repo => repo.AddAsync(It.Is<Room>(r =>
            r.Name == roomName &&
            r.UserId == userId
        )), Times.Once);
    }

    [Fact]
    public async Task DeleteRoomAsync_ShouldDeleteRoom_WhenRoomExistsAndBelongsToUser()
    {
        // ARRANGE
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var existingRoom = new Room { Id = roomId, UserId = userId, Name = "Test Room" };

        _roomRepoMock.Setup(r => r.GetByIdAsync(roomId))
                     .ReturnsAsync(existingRoom);

        _roomRepoMock.Setup(r => r.DeleteAsync(It.IsAny<Room>()))
                     .Returns(Task.CompletedTask);

        // ACT
        var result = await _roomService.DeleteRoomAsync(roomId, userId);

        // ASSERT
        result.Should().BeTrue();
        _roomRepoMock.Verify(repo => repo.DeleteAsync(It.Is<Room>(r => r.Id == roomId)), Times.Once);
    }

    [Fact]
    public async Task RenameRoomAsync_ShouldUpdateName_WhenRoomExists()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var oldRoom = new Room { Id = roomId, Name = "OldName", UserId = userId };

        // Mock GetById (Ownership check)
        _roomRepoMock.Setup(repo => repo.GetByIdAsync(roomId)).ReturnsAsync(oldRoom);
        
        _roomRepoMock.Setup(repo => repo.RoomNameExistsAsync(It.IsAny<string>(), userId))
                     .ReturnsAsync(false);
        
        _roomRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Room>())).Returns(Task.CompletedTask);

        var newName = "kitchen";

        // Act
        await _roomService.RenameRoomAsync(roomId, newName, userId);

        // Assert
        _roomRepoMock.Verify(repo => repo.UpdateAsync(It.Is<Room>(r =>
            r.Id == roomId &&
            r.Name == newName
        )), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnList_WhenRoomsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rooms = new List<Room>
        {
            new() { UserId = userId, Name = "Kitchen" },
            new() { UserId = userId, Name = "Bedroom" },
        };

        _roomRepoMock.Setup(repo => repo.GetAllByUserIdAsync(userId)).ReturnsAsync(rooms);

        // Act
        var result = await _roomService.GetAllAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Kitchen");
    }

    [Fact]
    public async Task RenameRoomAsync_ShouldReturnFalse_WhenRoomNotFound()
    {

        // Arrange
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _roomRepoMock.Setup(repo => repo.GetByIdAsync(roomId)).ReturnsAsync((Room?)null);

        // Act
        var result = await _roomService.RenameRoomAsync(roomId, "NewName", userId);

        // Assert
        result.Should().BeFalse(); // Service returns false, doesn't throw
        _roomRepoMock.Verify(repo => repo.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }
}