using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Services;
using Xunit;

namespace SmartHome.UnitTests.Services;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepoMock;
    private readonly Mock<IDeviceRepository> _deviceRepoMock;

    private readonly RoomService _roomService;

    public RoomServiceTests()
    {
        _roomRepoMock = new Mock<IRoomRepository>();
        _deviceRepoMock = new Mock<IDeviceRepository>();

        _roomService = new RoomService(_roomRepoMock.Object, _deviceRepoMock.Object);
    }

    [Fact]
    public void AddRoom_ShouldCallRepository_WhenNameIsValid()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var roomName = "Kitchen";

        // ACT
        _roomService.AddRoom(userId, roomName);

        // ASSERT
        // Sprawdzamy, czy metoda Add w repozytorium została wywołana RAZ
        // i czy przekazany obiekt Room ma dobrą nazwę i UserId.
        _roomRepoMock.Verify(repo => repo.Add(It.Is<Room>(r =>
            r.Name == roomName &&
            r.UserId == userId
        )), Times.Once);
    }
}