using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.Devices;
using SmartHome.Infrastructure.Services;

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

    // Creation - LightBulb
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task AddLightBulbAsync_ShouldThrowArgumentException_WhenNameIsInvalid(string name)
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Func<Task> action = async () => await _deviceService.AddLightBulbAsync(name, roomId, userId);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddLightBulbAsync_ShouldCreateLightBulb_AndAssignUserId_AndNotify()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var name = "Living Room Lamp";

        _deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<LightBulb>())).Returns(Task.CompletedTask);
        _deviceNotifierMock.Setup(n => n.NotifyDeviceChanged()).Returns(Task.CompletedTask);

        // Act
        var result = await _deviceService.AddLightBulbAsync(name, roomId, userId);

        // Assert
        result.Should().NotBeEmpty();
        _deviceRepoMock.Verify(r => r.AddAsync(It.Is<LightBulb>(d =>
            d.Name == name &&
            d.RoomId == roomId &&
            d.UserId == userId)), Times.Once);

        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    // Creation - TemperatureSensor
    [Fact]
    public async Task AddTemperatureSensorAsync_ShouldCreateSensor_AndAssignUserId_AndNotify()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var name = "Living Room Sensor";

        _deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<TemperatureSensor>())).Returns(Task.CompletedTask);
        _deviceNotifierMock.Setup(n => n.NotifyDeviceChanged()).Returns(Task.CompletedTask);

        // Act
        var result = await _deviceService.AddTemperatureSensorAsync(name, roomId, userId);

        // Assert
        result.Should().NotBeEmpty();
        _deviceRepoMock.Verify(r => r.AddAsync(It.Is<TemperatureSensor>(d =>
            d.Name == name &&
            d.RoomId == roomId &&
            d.UserId == userId)), Times.Once);

        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    // Business logic - Toggle light
    [Theory]
    [InlineData(false, true)] // turn off -> turn on
    [InlineData(true, false)] // turn on -> turn off
    public async Task TurnOnOffAsync_ShouldToggleState_AndNotify(bool initialIsOn, bool wantTurnOn)
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

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(bulb);
        _deviceRepoMock.Setup(r => r.UpdateAsync(bulb)).Returns(Task.CompletedTask);
        _deviceNotifierMock.Setup(n => n.NotifyDeviceChanged()).Returns(Task.CompletedTask);

        // Act
        bool result;
        if (wantTurnOn)
        {
            result = await _deviceService.TurnOnAsync(deviceId, userId);
        }
        else
        {
            result = await _deviceService.TurnOffAsync(deviceId, userId);
        }

        // Assert
        result.Should().BeTrue();
        bulb.IsOn.Should().Be(wantTurnOn);

        _deviceRepoMock.Verify(r => r.UpdateAsync(bulb), Times.Once);
        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    [Fact]
    public async Task TurnOnAsync_ShouldReturnFalse_WhenDeviceIsNotLightBulb()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sensor = new TemperatureSensor("Sensor", Guid.NewGuid()) { Id = deviceId, UserId = userId };

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(sensor);

        // Act
        var result = await _deviceService.TurnOnAsync(deviceId, userId);

        // Assert
        result.Should().BeFalse();
        _deviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Device>()), Times.Never);
    }

    // Getting devices
    [Fact]
    public async Task GetDeviceByIdAsync_ShouldReturnDevice_WhenUserIsOwner()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = new LightBulb("Lamp", Guid.NewGuid()) { Id = deviceId, UserId = userId };

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(device);

        // Act
        var result = await _deviceService.GetDeviceByIdAsync(deviceId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(device);
    }

    [Fact]
    public async Task GetDeviceByIdAsync_ShouldReturnNull_WhenUserIsNotOwner()
    {
        // Arrange
        var notOwnerUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, notOwnerUserId)).ReturnsAsync((Device?)null);

        // Act
        var result = await _deviceService.GetDeviceByIdAsync(deviceId, notOwnerUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllDevicesAsync_ShouldReturnFilteredList_WhenSearchProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var devicesList = new List<Device>
        {
            new LightBulb("Living Room Lamp", Guid.NewGuid()),
            new TemperatureSensor("Living Room Sensor", Guid.NewGuid())
        };

        _deviceRepoMock.Setup(r => r.GetAllByUserIdAsync(userId, "Living")).ReturnsAsync(devicesList);

        // Act
        var result = await _deviceService.GetAllDevicesAsync(userId, "Living");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllDevicesAsync_ShouldReturnAllDevices_WhenNoSearchProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var devicesList = new List<Device>
        {
            new LightBulb("Lamp", Guid.NewGuid()),
            new TemperatureSensor("Sensor", Guid.NewGuid())
        };

        _deviceRepoMock.Setup(r => r.GetAllByUserIdAsync(userId, null)).ReturnsAsync(devicesList);

        // Act
        var result = await _deviceService.GetAllDevicesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Lamp");
        result.Should().Contain(d => d.Name == "Sensor");
    }

    // Deletion
    [Fact]
    public async Task DeleteDeviceAsync_ShouldRemoveFromRepo_AndNotify_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = new LightBulb("Test", Guid.NewGuid()) { Id = deviceId, UserId = userId };

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(device);
        _deviceRepoMock.Setup(r => r.DeleteAsync(device)).Returns(Task.CompletedTask);
        _deviceNotifierMock.Setup(n => n.NotifyDeviceChanged()).Returns(Task.CompletedTask);

        // Act
        var result = await _deviceService.DeleteDeviceAsync(deviceId, userId);

        // Assert
        result.Should().BeTrue();
        _deviceRepoMock.Verify(r => r.DeleteAsync(device), Times.Once);
        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    [Fact]
    public async Task DeleteDeviceAsync_ShouldReturnFalse_WhenDeviceNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync((Device?)null);

        // Act
        var result = await _deviceService.DeleteDeviceAsync(deviceId, userId);

        // Assert
        result.Should().BeFalse();
        _deviceRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Device>()), Times.Never);
        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Never);
    }

    // Temperature
    [Fact]
    public async Task GetTemperatureAsync_ShouldReturnValue_WhenDeviceIsSensor()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sensor = new TemperatureSensor("Thermometer", Guid.NewGuid()) 
        { 
            Id = deviceId,
            UserId = userId 
        };
        sensor.SetTemperature(23);

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(sensor);

        // Act
        var result = await _deviceService.GetTemperatureAsync(deviceId, userId);

        // Assert
        result.Should().Be(23);
    }

    [Fact]
    public async Task GetTemperatureAsync_ShouldReturnNull_WhenDeviceIsNotSensor()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bulb = new LightBulb("Lamp", Guid.NewGuid()) { Id = deviceId, UserId = userId };

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(bulb);

        // Act
        var result = await _deviceService.GetTemperatureAsync(deviceId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTemperatureAsync_ShouldUpdateSensor_WhenDeviceIsSensor()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var sensor = new TemperatureSensor("T1", Guid.NewGuid()) { Id = deviceId };
        double newTemp = 24.5;

        _deviceRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { sensor });
        _deviceRepoMock.Setup(r => r.UpdateAsync(sensor)).Returns(Task.CompletedTask);

        // Act
        var result = await _deviceService.UpdateTemperatureAsync(deviceId, newTemp);

        // Assert
        result.Should().BeTrue();
        sensor.CurrentTemperature.Should().Be(newTemp);
        _deviceRepoMock.Verify(r => r.UpdateAsync(sensor), Times.Once);
    }

    [Fact]
    public async Task UpdateTemperatureAsync_ShouldReturnFalse_WhenDeviceIsNotSensor()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var bulb = new LightBulb("Lamp", Guid.NewGuid()) { Id = deviceId };

        _deviceRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { bulb });

        // Act
        var result = await _deviceService.UpdateTemperatureAsync(deviceId, 25);

        // Assert
        result.Should().BeFalse();
    }

    // Rename
    [Fact]
    public async Task RenameDeviceAsync_ShouldRenameDevice_AndNotify_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = new LightBulb("Old Name", Guid.NewGuid()) { Id = deviceId, UserId = userId };
        var newName = "New Name";

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync(device);
        _deviceRepoMock.Setup(r => r.UpdateAsync(device)).Returns(Task.CompletedTask);
        _deviceNotifierMock.Setup(n => n.NotifyDeviceChanged()).Returns(Task.CompletedTask);

        // Act
        var result = await _deviceService.RenameDeviceAsync(deviceId, newName, userId);

        // Assert
        result.Should().BeTrue();
        device.Name.Should().Be(newName);
        _deviceRepoMock.Verify(r => r.UpdateAsync(device), Times.Once);
        _deviceNotifierMock.Verify(n => n.NotifyDeviceChanged(), Times.Once);
    }

    [Fact]
    public async Task RenameDeviceAsync_ShouldReturnFalse_WhenDeviceNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _deviceRepoMock.Setup(r => r.GetAsync(deviceId, userId)).ReturnsAsync((Device?)null);

        // Act
        var result = await _deviceService.RenameDeviceAsync(deviceId, "New Name", userId);

        // Assert
        result.Should().BeFalse();
        _deviceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Device>()), Times.Never);
    }

    // Server-side operations
    [Fact]
    public async Task GetAllServersSideAsync_ShouldReturnAllDevices_RegardlessOfUser()
    {
        // Arrange
        var list = new List<Device>
        {
            new LightBulb("Lamp1", Guid.NewGuid()),
            new TemperatureSensor("Sensor1", Guid.NewGuid())
        };

        _deviceRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

        // Act
        var result = await _deviceService.GetAllServersSideAsync();

        // Assert
        result.Should().HaveCount(2);
    }
}