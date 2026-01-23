using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Services;
using Xunit;

namespace SmartHome.UnitTests.Services;

public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly Mock<IDeviceNotifier> _deviceNotifierMock;

    private readonly DeviceService _deviceService;

    public DeviceServiceTests()
    {
        _deviceRepoMock = new Mock<IDeviceRepository>();
        _deviceNotifierMock = new Mock<IDeviceNotifier>();

        _deviceService = new DeviceService(_deviceRepoMock.Object, _deviceNotifierMock.Object);
    }

    // Creation - (factory & validation)
    [Theory]
    [InlineData("", "LightBulb")]          // Empty name
    [InlineData("Lampa", "Toaster")]       // unknown device type
    public void AddDevice_ShouldThrowArgumentException_WhenInputIsInvalid(string name, string type)
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Action action = () => _deviceService.AddDevice(name, roomId, type, userId);

        action.Should().Throw<ArgumentException>();
    }

    // this test is crucial - checks if: switch works correctly,
    // userId is assigned to created obj, notifies go to the frontend
    [Theory]
    [InlineData("LightBulb", typeof(LightBulb))]
    [InlineData("TemperatureSensor", typeof(TemperatureSensor))]
    [InlineData("lightbulb", typeof(LightBulb))]
    public void AddDevice_ShouldCreateCorrectType_AndAssignUserId(string type, Type expectedType)
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var name = "Test Device";

        _deviceService.AddDevice(name, roomId, type, userId);

        _deviceRepoMock.Verify(repo => repo.Add(It.Is<Device>(d => d.Name == name &&
            d.RoomId == roomId &&
            d.UserId == userId &&
            d.GetType() == expectedType
            )), Times.Once);

        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    // business logic (toggle lamp light)
    [Theory]
    [InlineData(false, true)] // turn off -> turn on
    [InlineData(true, false)] // turn on > turn off
    public void ToggleDevice_ShouldFlipState_AndNotify(bool initialIsOn, bool wantTurnOn)
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bulb = new LightBulb("Lamp", Guid.NewGuid())
        {
            Id = deviceId,
            UserId = userId,
        };

        if (initialIsOn) bulb.TurnOn();
        else bulb.TurnOff();

        _deviceRepoMock.Setup(repo => repo.Get(deviceId, userId)).Returns(bulb);

        // Act
        if (wantTurnOn)
        {
            _deviceService.TurnOn(deviceId, userId);
        }
        else
        {
            _deviceService.TurnOff(deviceId, userId);
        }

        // Assert
        bulb.IsOn.Should().Be(wantTurnOn);

        _deviceRepoMock.Verify(repo => repo.Update(bulb), Times.Once);
        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    [Fact]
    public void GetDevice_ShouldReturnNull_WhenUserIsNotOwner()
    {
        // Arrange
        var notOwnerUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        // Teach
        _deviceRepoMock.Setup(repo => repo.Get(deviceId, notOwnerUserId)).Returns((Device?)null);

        // Act
        var result = _deviceService.GetDeviceById(deviceId, notOwnerUserId);

        result.Should().BeNull();
    }

    // removing 
    [Fact]
    public void DeleteDevice_ShouldRemoveFromRepo_AndNotify_WhenUserIsOwner()
    {
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = new LightBulb("Test", Guid.NewGuid());

        _deviceRepoMock.Setup(r => r.Get(deviceId, userId)).Returns(device);

        var result = _deviceService.DeleteDevice(deviceId, userId);

        result.Should().BeTrue();

        _deviceRepoMock.Verify(r => r.Delete(deviceId), Times.Once);
        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    [Fact]
    public void DeleteDevice_ShouldNotCallDelete_WhenDeviceNotFound()
    {
        // ARRANGE
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _deviceRepoMock.Setup(r => r.Get(deviceId, userId)).Returns((Device?)null);

        // ACT
        var result = _deviceService.DeleteDevice(deviceId, userId);

        // ASSERT
        result.Should().BeFalse();

        // we assert that delete method was not called
        // we should not delete somethin which we didn't find.
        _deviceRepoMock.Verify(r => r.Delete(It.IsAny<Guid>()), Times.Never);

        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Never);
    }

    // getting
    [Fact]
    public void GetAllDevices_ShouldReturnList_FromRepository()
    {
        var userId = Guid.NewGuid();
        var devicesList = new List<Device>
        {
            new LightBulb("Lamp", Guid.NewGuid()),
            new TemperatureSensor("Sensor", Guid.NewGuid())
        };

        _deviceRepoMock.Setup(r => r.GetAll(userId)).Returns(devicesList);

        var result = _deviceService.GetAllDevices(userId);

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Lamp");
        result.Should().Contain(d => d.Name == "Sensor");
    }

    [Fact]
    public void GetTemperature_ShouldReturnValue_WhenDeviceIsSensor()
    {
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sensor = new TemperatureSensor("Termometr", Guid.NewGuid()) { UserId = userId };
        sensor.SetTemperature(23);

        _deviceRepoMock.Setup(r => r.Get(deviceId, userId)).Returns(sensor);

        var result = _deviceService.GetTemperature(deviceId, userId);

        result.Should().NotBeNull();
        result.Should().Be(23);
    }

    [Fact]
    public void GetTemperature_ShouldReturnNull_WhenDeviceIsNotSensor()
    {
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bulb = new LightBulb("Lamp", Guid.NewGuid()) { UserId = userId };

        _deviceRepoMock.Setup(r => r.Get(deviceId, userId)).Returns(bulb);

        var result = _deviceService.GetTemperature(deviceId, userId);

        result.Should().BeNull();
    }

    [Fact]
    public void GetAllServersSide_ShouldReturnAllDevices_RegardlessOfUser()
    {
        // Arrange
        var list = new List<Device>
        {
            new LightBulb("Lamp1", Guid.NewGuid()),
            new TemperatureSensor("Sensor1", Guid.NewGuid())
        };

        _deviceRepoMock.Setup(r => r.GetAllServersSide()).Returns(list);

        // Act
        var result = _deviceService.GetAllServersSide();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateTemperature_ShouldCallRepo_WhenDeviceIsSensor()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sensor = new TemperatureSensor("T1", Guid.NewGuid()) { Id = deviceId };

        double newTemp = 24.5;

        _deviceRepoMock.Setup(r => r.GetAllServersSide())
                       .Returns([sensor]);

        // Act
        _deviceService.UpdateTemperature(deviceId, newTemp);

        // Assert
        sensor.CurrentTemperature.Should().Be(newTemp);
        _deviceRepoMock.Verify(r => r.Update(sensor), Times.Once);
    }
}