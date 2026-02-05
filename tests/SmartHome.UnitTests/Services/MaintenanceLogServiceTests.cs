using FluentAssertions;
using Moq;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces;
using SmartHome.Infrastructure.Services;

namespace SmartHome.UnitTests.Services;

public class MaintenanceLogServiceTests
{
    private readonly Mock<IMaintenanceLogRepository> _logRepoMock;
    private readonly MaintenanceLogService _service;

    public MaintenanceLogServiceTests()
    {
        _logRepoMock = new Mock<IMaintenanceLogRepository>();
        _service = new MaintenanceLogService(_logRepoMock.Object);
    }

    // add
    [Fact]
    public void AddLog_ShouldCreateLog_AndCallRepository()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        string title = "Battery change";
        string desc = "Battery CR2032 changed with a new one (the same model).";

        // Act
        _service.AddLog(deviceId, title, desc);

        // Assert
        // check if object was sent to repo with correct data
        _logRepoMock.Verify(repo => repo.Add(It.Is<MaintenanceLog>(log =>
            log.DeviceId == deviceId &&
            log.Title == title &&
            log.Description == desc &&
            log.CreatedAt != default
        )), Times.Once);
    }

    // get
    [Fact]
    public void GetLogsForDevice_ShouldReturnList_WhenLogsExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var logs = new List<MaintenanceLog>
        {
            new() { Id = Guid.NewGuid(), Title = "Damage 1" },
            new() { Id = Guid.NewGuid(), Title = "Damage 2" }
        };

        _logRepoMock.Setup(r => r.GetByDeviceId(deviceId)).Returns(logs);

        // Act
        var result = _service.GetLogsForDevice(deviceId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Title == "Damage 1");
        result.Should().Contain(l => l.Title == "Damage 2");
    }

    [Fact]
    public void GetLogsForDevice_ShouldReturnEmptyList_WhenNoLogsFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        _logRepoMock.Setup(r => r.GetByDeviceId(deviceId)).Returns([]);

        // Act
        var result = _service.GetLogsForDevice(deviceId);

        // Assert
        result.Should().BeEmpty();
    }

    // update

    [Fact]
    public void UpdateLog_ShouldUpdateFields_WhenLogExists()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var existingLog = new MaintenanceLog
        {
            Id = logId,
            Title = "Old Title",
            Description = "Old description"
        };

        _logRepoMock.Setup(r => r.GetById(logId)).Returns(existingLog);

        // Act
        _service.UpdateLog(logId, "New Title", "New Description");

        // Assert
        // Is object in memory changed?
        existingLog.Title.Should().Be("New Title");
        existingLog.Description.Should().Be("New Description");

        // Did repo saved changes?
        _logRepoMock.Verify(r => r.Update(existingLog), Times.Once);
    }

    [Fact]
    public void UpdateLog_ShouldThrowException_WhenLogNotFound()
    {
        // Arrange
        var logId = Guid.NewGuid();
        _logRepoMock.Setup(r => r.GetById(logId)).Returns((MaintenanceLog?)null);

        // Act
        Action action = () => _service.UpdateLog(logId, "Title", "Description");

        // Assert
        action.Should().Throw<Exception>();

        _logRepoMock.Verify(r => r.Update(It.IsAny<MaintenanceLog>()), Times.Never);
    }

    // delete
    [Fact]
    public void DeleteLog_ShouldCallDelete_WhenLogExists()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = new MaintenanceLog { Id = logId };

        _logRepoMock.Setup(r => r.GetById(logId)).Returns(log);

        // Act
        _service.DeleteLog(logId);

        // Assert
        _logRepoMock.Verify(r => r.Delete(log), Times.Once);
    }

    [Fact]
    public void DeleteLog_ShouldThrowException_WhenLogNotFound()
    {
        // Arrange
        var logId = Guid.NewGuid();
        _logRepoMock.Setup(r => r.GetById(logId)).Returns((MaintenanceLog?)null);

        // Act
        Action action = () => _service.DeleteLog(logId);

        // Assert
        action.Should().Throw<Exception>();
        _logRepoMock.Verify(r => r.Delete(It.IsAny<MaintenanceLog>()), Times.Never);
    }
}