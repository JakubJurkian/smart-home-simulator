using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class MaintenanceLogsControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(string deviceId, string logId)> SetupDeviceWithLogAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var email = $"servicer-{uniqueSuffix}@test.com";

        // Auth
        await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest("ServiceTech", email, "Pass123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest(email, "Pass123!"));
        loginResponse.EnsureSuccessStatusCode();

        // Create Room
        await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bedroom"));

        // Get RoomId
        var roomsRes = await _client.GetAsync("/api/rooms");
        var rooms = await roomsRes.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        var roomId = rooms!.First(r => r.Name == "Bedroom").Id;

        // Create Device
        var newDevice = new CreateDeviceRequest("Sensor 1", Guid.Parse(roomId), "TemperatureSensor");
        await _client.PostAsJsonAsync("/api/devices", newDevice);

        // Get DeviceId
        var devicesRes = await _client.GetAsync("/api/devices");
        var devices = await devicesRes.Content.ReadFromJsonAsync<List<TestDeviceDto>>();
        var deviceId = devices!.First(d => d.Name == "Sensor 1").Id;

        // Create Log
        var newLog = new CreateLogRequest(Guid.Parse(deviceId), "Info", "Initial Log");
        await _client.PostAsJsonAsync("/api/logs", newLog);

        // Get LogId
        var logsRes = await _client.GetAsync($"/api/logs/{deviceId}");
        var logs = await logsRes.Content.ReadFromJsonAsync<List<TestLogDto>>();
        var logId = logs!.First(l => l.Description == "Initial Log").Id;

        return (deviceId, logId);
    }

    [Fact]
    public async Task CreateLog_ShouldAddLogToDevice_WhenUserIsLoggedIn()
    {
        // Arrange
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var email = $"servicer-{uniqueSuffix}@test.com";

        await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest("ServiceTech", email, "Pass123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest(email, "Pass123!"));
        loginResponse.EnsureSuccessStatusCode();

        await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bedroom"));

        var roomsRes = await _client.GetAsync("/api/rooms");
        var rooms = await roomsRes.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        var roomId = rooms!.First(r => r.Name == "Bedroom").Id;

        var newDevice = new CreateDeviceRequest("Sensor 1", Guid.Parse(roomId), "TemperatureSensor");
        await _client.PostAsJsonAsync("/api/devices", newDevice);

        var devicesRes = await _client.GetAsync("/api/devices");
        var devices = await devicesRes.Content.ReadFromJsonAsync<List<TestDeviceDto>>();
        var deviceId = devices!.First(d => d.Name == "Sensor 1").Id;

        // Act
        var newLog = new CreateLogRequest(Guid.Parse(deviceId), "Info", "Routine Checkup");
        var logResponse = await _client.PostAsJsonAsync("/api/logs", newLog);

        // Assert
        logResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getLogsResponse = await _client.GetAsync($"/api/logs/{deviceId}");
        getLogsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await getLogsResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Routine Checkup");
    }

    #region UpdateLog Tests

    [Fact]
    public async Task UpdateLog_ShouldSucceed_WhenLogExists()
    {
        // Arrange
        var (deviceId, logId) = await SetupDeviceWithLogAsync();
        var updateRequest = new UpdateLogRequest("Updated Title", "Updated Description");

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/logs/{logId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logsRes = await _client.GetAsync($"/api/logs/{deviceId}");
        var content = await logsRes.Content.ReadAsStringAsync();
        content.Should().Contain("Updated Description");
        content.Should().NotContain("Initial Log");
    }

    [Fact]
    public async Task UpdateLog_ShouldFail_WhenLogDoesNotExist()
    {
        // Arrange
        await SetupDeviceWithLogAsync();
        var nonExistentLogId = Guid.NewGuid();
        var updateRequest = new UpdateLogRequest("Title", "Description");

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/logs/{nonExistentLogId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DeleteLog Tests

    [Fact]
    public async Task DeleteLog_ShouldSucceed_WhenLogExists()
    {
        // Arrange
        var (deviceId, logId) = await SetupDeviceWithLogAsync();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/logs/{logId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var logsRes = await _client.GetAsync($"/api/logs/{deviceId}");
        var content = await logsRes.Content.ReadAsStringAsync();
        content.Should().NotContain("Initial Log");
    }

    [Fact]
    public async Task DeleteLog_ShouldFail_WhenLogDoesNotExist()
    {
        // Arrange
        await SetupDeviceWithLogAsync();
        var nonExistentLogId = Guid.NewGuid();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/logs/{nonExistentLogId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}

internal record TestLogDto(string Id, string Title, string Description);