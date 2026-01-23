using FluentAssertions; // allows to write english-like code
using Moq; // creates repositories mocks (objects) to not connect with real db

using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Services;

namespace SmartHome.UnitTests.Services;

public class RoomServiceTests
{
    // mocking dependencies
    private readonly Mock<IRoomRepository> _roomRepoMock;
    private readonly Mock<IDeviceRepository> _deviceRepoMock;

    // system under test (sut)
    private readonly RoomService _roomService;

    public RoomServiceTests()
    {
        _roomRepoMock = new Mock<IRoomRepository>(); // dynamic proxy (object that mocks other object)
        _deviceRepoMock = new Mock<IDeviceRepository>();
        // this way, cause for every test ([fact]) xUnit creates
        // new instance of RoomServiceTests class

        // inject fake repo to real service
        _roomService = new RoomService(_roomRepoMock.Object, _deviceRepoMock.Object);
    }

    [Fact] // defines method below as a TEST.
    // check if Add method was called in repo ONCE
    // and check if Room obj has valid name & userId
    public void AddRoom_ShouldCallRepository_WhenNameIsValid()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var roomName = "Kitchen";

        // ACT
        _roomService.AddRoom(userId, roomName);

        // ASSERT
        _roomRepoMock.Verify(repo => repo.Add(It.Is<Room>(r =>
            r.Name == roomName &&
            r.UserId == userId
        )), Times.Once);
        // side-effects & communcation contract is checked
    }

    [Fact]
    public void DeleteRoom_ShouldDeleteDevicesFirst_ThenDeleteRoom()
    {
        var roomId = Guid.NewGuid();

        _roomService.DeleteRoom(roomId);

        _deviceRepoMock.Verify(repo => repo.DeleteAllByRoomId(roomId), Times.Once);
        _roomRepoMock.Verify(repo => repo.Delete(roomId), Times.Once);
    }

    [Fact]
    public void RenameRoom_ShouldUpdateName_WhenRoomExists()
    {
        var roomId = Guid.NewGuid();
        var oldRoom = new Room { Id = roomId, Name = "OldName" };

        // we teach mock: if ask for that room, return this room
        _roomRepoMock.Setup(repo => repo.GetById(roomId)).Returns(oldRoom);

        var newName = "kitchen";
        _roomService.RenameRoom(roomId, newName);

        // check if update method has the room with changed name
        _roomRepoMock.Verify(repo => repo.Update(It.Is<Room>(r =>
        r.Id == roomId &&
        r.Name == newName
        )), Times.Once);
    }

    [Fact]
    public void GetUserRooms_ShouldReturnList_WhenRoomsExist()
    {
        var userId = Guid.NewGuid();
        var rooms = new List<Room>
        {
            new() { UserId = userId, Name = "Kitchen" },
            new() { UserId = userId, Name = "Bedroom" },
        };

        // we teach mock: if asked for the user's rooms, return this list
        _roomRepoMock.Setup(repo => repo.GetAllByUserId(userId)).Returns(rooms);

        var result = _roomService.GetUserRooms(userId);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Kitchen");
    }
}