using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class DevicesControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    // The HttpClient created by the factory handles cookies by default.
    // If we log in during the first step, the client retains the session for subsequent requests.
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateDevice_ShouldAddDeviceToRoom_WhenUserIsLoggedIn()
    {
        // Arrange - authentication
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var email = $"tech-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest("TechGuy", email, password));
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        // Arrange - create room (To assign the device to)
        var newRoom = new CreateRoomRequest("Bedroom");
        var createRoomResponse = await _client.PostAsJsonAsync("/api/rooms", newRoom);
        createRoomResponse.EnsureSuccessStatusCode();

        var getAllRoomsResponse = await _client.GetAsync("/api/rooms");
        var rooms = await getAllRoomsResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();

        var createdRoom = rooms!.First(r => r.Name == "Bedroom");
        var roomId = createdRoom.Id;

        // Act - create device
        var newDevice = new CreateDeviceRequest("Lamp 1", Guid.Parse(roomId), "LightBulb");

        var deviceResponse = await _client.PostAsJsonAsync("/api/devices", newDevice);

        // Assert
        deviceResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync("/api/devices");
        var content = await getResponse.Content.ReadAsStringAsync();

        // Verify that the created device is in the list
        content.Should().Contain("Lamp 1");
    }

    [Fact]
    public async Task DeleteDevice_ShouldRemoveDevice_WhenUserIsLoggedIn()
    {
        // Arrange - setup (auth + room + device)
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var email = $"deleter-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        // Auth
        await _client.PostAsJsonAsync("/api/users/register", new RegisterRequest("Deleter", email, password));
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        // Create Room
        var roomResponse = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Garbage Room"));
        roomResponse.EnsureSuccessStatusCode();

        // Get RoomId (Helper logic from previous test)
        var getAllRoomsResponse = await _client.GetAsync("/api/rooms");
        var rooms = await getAllRoomsResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        var roomId = rooms!.First(r => r.Name == "Garbage Room").Id;

        // Create Device
        var deviceRequest = new CreateDeviceRequest("Trash Lamp", Guid.Parse(roomId), "LightBulb");
        var createDevResponse = await _client.PostAsJsonAsync("/api/devices", deviceRequest);
        createDevResponse.EnsureSuccessStatusCode();

        // Get Device ID (We need it to delete)
        var getAllDevicesResponse = await _client.GetAsync("/api/devices");
        var content = await getAllDevicesResponse.Content.ReadAsStringAsync();

        var devices = await getAllDevicesResponse.Content.ReadFromJsonAsync<List<TestDeviceDto>>();
        var deviceId = devices!.First(d => d.Name == "Trash Lamp").Id;

        // Act - delete
        var deleteResponse = await _client.DeleteAsync($"/api/devices/{deviceId}");

        // Assert
        deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        // Verify it's gone
        var checkResponse = await _client.GetAsync($"/api/devices/{deviceId}");
        // API usually returns 404 Not Found if device doesn't exist
        checkResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

internal record TestIdDto(Guid Id);
internal record TestDeviceDto(string Id, string Name);