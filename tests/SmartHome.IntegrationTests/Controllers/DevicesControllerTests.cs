using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces.Devices;
using System.Text.Json; // Needed for JsonElement
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class DevicesControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // API Endpoints
    private const string DevicesBase = "/api/devices";
    private const string RoomsBase = "/api/rooms";
    private const string UsersRegister = "/api/users/register";
    private const string UsersLogin = "/api/users/login";

    #region Create & Get (Happy Path)

    [Fact]
    public async Task CreateDevice_ShouldAddDeviceToRoom_WhenUserIsLoggedIn()
    {
        await RegisterAndLoginAsync("tech");
        var roomId = await CreateRoomAsync("Bedroom");

        var newDevice = new CreateDeviceRequest { Name = "Lamp 1", RoomId = Guid.Parse(roomId), Type = "LightBulb" };

        // Correct Endpoint
        var deviceResponse = await _client.PostAsJsonAsync($"{DevicesBase}/lightbulb", newDevice);

        deviceResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"{DevicesBase}/all-system");
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Lamp 1");
    }

    [Fact]
    public async Task GetDevicesByUserId_ShouldReturnList_ContainingUserDevices()
    {
        var userId = await RegisterAndLoginAsync("tech");

        var roomId = await CreateRoomAsync("Bedroom");

        var newDevice = new CreateDeviceRequest { Name = "Lamp 1", RoomId = Guid.Parse(roomId), Type = "LightBulb" };
        var createResponse = await _client.PostAsJsonAsync($"{DevicesBase}/lightbulb", newDevice);
        createResponse.EnsureSuccessStatusCode();

        // controller knows the userId thanks to cookie from login, so we just hit the base endpoint
        var getResponse = await _client.GetAsync(DevicesBase);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var devices = await getResponse.Content.ReadFromJsonAsync<List<TestDeviceDto>>();

        devices.Should().NotBeNull();
        devices.Should().Contain(d => d.Name == "Lamp 1");
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteDevice_ShouldRemoveDevice_WhenUserIsLoggedIn()
    {
        await RegisterAndLoginAsync("deleter");
        var roomId = await CreateRoomAsync("Garbage Room");
        var deviceId = await CreateDeviceAsync("Trash Lamp", "LightBulb", roomId);

        var deleteResponse = await _client.DeleteAsync($"{DevicesBase}/{deviceId}");

        deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var checkResponse = await _client.GetAsync($"{DevicesBase}/{deviceId}");
        checkResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDevice_ShouldReturn400Or404_WhenDeviceNotFound()
    {
        await RegisterAndLoginAsync("fail-deleter");
        var deleteResponse = await _client.DeleteAsync($"{DevicesBase}/{Guid.NewGuid()}");
        deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Device Logic (Turn On/Off)

    [Fact]
    public async Task TurnOn_ShouldUpdateState_WhenDeviceIsBulb()
    {
        await RegisterAndLoginAsync("light-user");
        var roomId = await CreateRoomAsync("Living Room");
        var deviceId = await CreateDeviceAsync("Main Lamp", "LightBulb", roomId);

        var onResponse = await _client.PutAsync($"{DevicesBase}/{deviceId}/turn-on", null);
        onResponse.EnsureSuccessStatusCode();

        var deviceOn = await _client.GetFromJsonAsync<TestDeviceDto>($"{DevicesBase}/{deviceId}");
        deviceOn!.IsOn.Should().BeTrue();

        var offResponse = await _client.PutAsync($"{DevicesBase}/{deviceId}/turn-off", null);
        offResponse.EnsureSuccessStatusCode();

        var deviceOff = await _client.GetFromJsonAsync<TestDeviceDto>($"{DevicesBase}/{deviceId}");
        deviceOff!.IsOn.Should().BeFalse();
    }

    [Fact]
    public async Task TurnOn_ShouldReturn400_WhenDeviceIsSensor()
    {
        await RegisterAndLoginAsync("sensor-user");
        var roomId = await CreateRoomAsync("Kitchen");
        var deviceId = await CreateDeviceAsync("Oven Sensor", "TemperatureSensor", roomId);

        var response = await _client.PutAsync($"{DevicesBase}/{deviceId}/turn-on", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Device Logic (Temperature GET)

    [Fact]
    public async Task GetTemperature_ShouldReturnData_WhenDeviceIsSensor()
    {
        // Arrange - API setup
        await RegisterAndLoginAsync("temp-getter");
        var roomId = await CreateRoomAsync("Cold Room");
        var deviceIdString = await CreateDeviceAsync("Thermometer", "TemperatureSensor", roomId);
        var deviceId = Guid.Parse(deviceIdString);

        // Simulate Background Process
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDeviceService>();
            await service.UpdateTemperatureAsync(deviceId, 21.5);
        }

        // Act - User reads via API
        var response = await _client.GetAsync($"{DevicesBase}/{deviceId}/temperature");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TestTemperatureResponse>();
        result.Should().NotBeNull();
        result!.Unit.Should().Be("Celsius");
        result.Temperature.Should().Be(21.5);
    }

    [Fact]
    public async Task GetTemperature_ShouldReturn400_WhenDeviceIsBulb()
    {
        await RegisterAndLoginAsync("wrong-device-user");
        var roomId = await CreateRoomAsync("Dark Room");
        var deviceId = await CreateDeviceAsync("Just A Lamp", "LightBulb", roomId);

        var response = await _client.GetAsync($"{DevicesBase}/{deviceId}/temperature");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTemperature_ShouldReturn400_WhenDeviceNotFound()
    {
        await RegisterAndLoginAsync("404-user");
        var response = await _client.GetAsync($"{DevicesBase}/{Guid.NewGuid()}/temperature");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region System Admin (All Devices)

    [Fact]
    public async Task GetAllSystemDevices_ShouldReturnList_ContainingCreatedDevices()
    {
        await RegisterAndLoginAsync("admin-viewer");
        var roomId = await CreateRoomAsync("Server Room");

        await CreateDeviceAsync("SysSensor", "TemperatureSensor", roomId);
        await CreateDeviceAsync("SysBulb", "LightBulb", roomId);

        var response = await _client.GetAsync($"{DevicesBase}/all-system");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var allDevices = await response.Content.ReadFromJsonAsync<List<TestDeviceDto>>();
        allDevices.Should().NotBeNull();
        allDevices.Should().Contain(d => d.Name == "SysSensor");
        allDevices.Should().Contain(d => d.Name == "SysBulb");

        var sensor = allDevices!.First(d => d.Name == "SysSensor");
        sensor.RoomId.Should().Be(Guid.Parse(roomId));
    }

    #endregion

    #region Rename Device (PUT /api/devices/{id})

    [Fact]
    public async Task RenameDevice_ShouldReturnOk_WhenInputIsValid()
    {
        // 1. Arrange
        await RegisterAndLoginAsync("rename-success");
        var roomId = await CreateRoomAsync("Rename Room");
        var deviceIdString = await CreateDeviceAsync("Old Name", "LightBulb", roomId);
        var deviceId = Guid.Parse(deviceIdString);

        var newName = "Kitchen Light Updated";

        // 2. Act
        // Send JSON object
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", new { Name = newName });

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Additional check
        var getResponse = await _client.GetFromJsonAsync<TestDeviceDto>($"{DevicesBase}/{deviceId}");
        getResponse.Should().NotBeNull();
        getResponse!.Name.Should().Be(newName);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        // 1. Arrange
        await RegisterAndLoginAsync("rename-empty");
        var roomId = await CreateRoomAsync("Empty Name Room");
        var deviceId = await CreateDeviceAsync("Valid Name", "LightBulb", roomId);

        // 2. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", new { Name = "" });

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnNotFound_WhenDeviceDoesNotExist()
    {
        // 1. Arrange
        await RegisterAndLoginAsync("rename-404");
        var randomId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{randomId}", new { Name = "New Name" });

        // 3. Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnBadRequest_WhenDeviceBelongsToAnotherUser()
    {
        // 1. Arrange - User A
        await RegisterAndLoginAsync("user-owner");
        var roomId = await CreateRoomAsync("User A Room");
        var deviceId = await CreateDeviceAsync("User A Device", "LightBulb", roomId);

        // 2. Arrange - User B
        await RegisterAndLoginAsync("user-hacker");

        // 3. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", new { Name = "Hacked Name" });

        // 4. Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnUnauthorized_WhenNotLoggedIn()
    {
        // 1. Arrange
        var deviceId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", new { Name = "New Name" });

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    private async Task<Guid> RegisterAndLoginAsync(string prefix)
    {
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 6);
        var email = $"{prefix}-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        var registerResponse = await _client.PostAsJsonAsync(UsersRegister,
            new RegisterRequest { Username = $"{prefix}User", Email = email, Password = password });
        registerResponse.EnsureSuccessStatusCode();

        // Safe parsing of the anonymous result object from controller
        var result = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();

        if (result.TryGetProperty("id", out var idElement) && idElement.GetString() is string idStr)
        {
            // Login to set cookie
            var loginResponse = await _client.PostAsJsonAsync(UsersLogin,
                new LoginRequest { Email = email, Password = password });
            loginResponse.EnsureSuccessStatusCode();

            return Guid.Parse(idStr);
        }

        throw new Exception("Failed to parse User ID from registration response");
    }

    private async Task<string> CreateRoomAsync(string name)
    {
        await _client.PostAsJsonAsync(RoomsBase, new CreateRoomRequest { Name = name });
        var response = await _client.GetAsync(RoomsBase);
        var rooms = await response.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        return rooms!.First(r => r.Name == name).Id;
    }

    private async Task<string> CreateDeviceAsync(string name, string type, string roomId)
    {
        var endpoint = type switch
        {
            "LightBulb" => $"{DevicesBase}/lightbulb",
            "TemperatureSensor" => $"{DevicesBase}/temperaturesensor",
            _ => throw new ArgumentException($"Unknown device type: {type}")
        };

        var response = await _client.PostAsJsonAsync(endpoint, new CreateDeviceRequest { Name = name, RoomId = Guid.Parse(roomId), Type = type });
        response.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync($"{DevicesBase}/all-system");
        var devices = await getResponse.Content.ReadFromJsonAsync<List<TestDeviceDto>>();

        var device = devices!.FirstOrDefault(d => d.Name == name);
        if (device == null) throw new Exception($"Device '{name}' not found.");
        return device.Id;
    }

    #endregion

    #region Create (Sad Paths)

    [Fact]
    public async Task AddDevice_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        await RegisterAndLoginAsync("bad-name-user");
        var roomId = await CreateRoomAsync("Test Room");

        // Act
        var invalidDeviceRequest = new CreateDeviceRequest { Name = "", RoomId = Guid.Parse(roomId), Type = "LightBulb" };
        var response = await _client.PostAsJsonAsync($"{DevicesBase}/lightbulb", invalidDeviceRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task AddDevice_ShouldReturnUnauthorized_WhenNotLoggedIn()
    {
        // Arrange
        var request = new CreateDeviceRequest { Name = "Secret Lamp", RoomId = Guid.NewGuid(), Type = "LightBulb" };

        // Act
        var response = await _client.PostAsJsonAsync($"{DevicesBase}/lightbulb", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}

// DTOs
internal record TestIdDto(Guid Id);
internal record TestDeviceDto(string Id, string Name, Guid RoomId, string Type, bool? IsOn, double? CurrentTemperature);
internal record TestTemperatureResponse(double Temperature, string Unit);
internal record UpdateDeviceRequest(string Name, Guid DeviceId);