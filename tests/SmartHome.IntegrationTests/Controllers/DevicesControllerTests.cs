using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartHome.Api.Dtos;
using SmartHome.Domain.Interfaces;
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

        var newDevice = new CreateDeviceRequest("Lamp 1", Guid.Parse(roomId), "LightBulb");
        var deviceResponse = await _client.PostAsJsonAsync(DevicesBase, newDevice);

        deviceResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync(DevicesBase);
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Lamp 1");
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
            service.UpdateTemperature(deviceId, 21.5);
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
        // Wysyłamy string jako JSON (z cudzysłowami), bo kontroler oczekuje [FromBody] string
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", newName);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Dodatkowe sprawdzenie: Pobieramy urządzenie, by upewnić się, że nazwa w bazie się zmieniła
        var getResponse = await _client.GetFromJsonAsync<TestDeviceDto>($"{DevicesBase}/{deviceId}");
        getResponse.Should().NotBeNull();
        getResponse!.Name.Should().Be(newName);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        // Pokrywa: catch (ArgumentException ex) -> BadRequest
        // Logika w encji Device: if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException...

        // 1. Arrange
        await RegisterAndLoginAsync("rename-empty");
        var roomId = await CreateRoomAsync("Empty Name Room");
        var deviceId = await CreateDeviceAsync("Valid Name", "LightBulb", roomId);

        // 2. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", "   "); // Pusty string/spacje

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Device name cannot be empty");
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnNotFound_WhenDeviceDoesNotExist()
    {
        // Pokrywa: if (!success) -> return NotFound();
        // Service zwraca false, gdy repozytorium zwróci null

        // 1. Arrange
        await RegisterAndLoginAsync("rename-404");
        var randomId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{randomId}", "New Name");

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnNotFound_WhenDeviceBelongsToAnotherUser()
    {
        // Pokrywa: bezpieczeństwo danych. Użytkownik A nie może zmienić nazwy urządzenia Użytkownika B.
        // Service.Get(id, userId) zwróci null, więc success = false -> NotFound

        // 1. Arrange - User A tworzy urządzenie
        await RegisterAndLoginAsync("user-owner");
        var roomId = await CreateRoomAsync("User A Room");
        var deviceId = await CreateDeviceAsync("User A Device", "LightBulb", roomId);

        // 2. Arrange - Logujemy się jako User B (Hacker)
        await RegisterAndLoginAsync("user-hacker");

        // 3. Act - User B próbuje zmienić nazwę urządzenia Usera A
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", "Hacked Name");

        // 4. Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenameDevice_ShouldReturnUnauthorized_WhenNotLoggedIn()
    {
        // Pokrywa: catch (UnauthorizedAccessException) -> Unauthorized
        
        // 1. Arrange
        // NIE logujemy się (brak ciasteczka)
        var deviceId = Guid.NewGuid();

        // 2. Act
        var response = await _client.PutAsJsonAsync($"{DevicesBase}/{deviceId}", "New Name");

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    private async Task RegisterAndLoginAsync(string prefix)
    {
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 6);
        var email = $"{prefix}-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        await _client.PostAsJsonAsync(UsersRegister, new RegisterRequest($"{prefix}User", email, password));
        var loginResponse = await _client.PostAsJsonAsync(UsersLogin, new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
    }

    private async Task<string> CreateRoomAsync(string name)
    {
        await _client.PostAsJsonAsync(RoomsBase, new CreateRoomRequest(name));
        var response = await _client.GetAsync(RoomsBase);
        var rooms = await response.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        return rooms!.First(r => r.Name == name).Id;
    }

    private async Task<string> CreateDeviceAsync(string name, string type, string roomId)
    {
        await _client.PostAsJsonAsync(DevicesBase, new CreateDeviceRequest(name, Guid.Parse(roomId), type));
        var response = await _client.GetAsync(DevicesBase);
        var devices = await response.Content.ReadFromJsonAsync<List<TestDeviceDto>>();
        return devices!.First(d => d.Name == name).Id;
    }

    #endregion

    #region Create (Sad Paths)

    [Fact]
    public async Task AddDevice_ShouldReturnBadRequest_WhenDeviceTypeIsInvalid()
    {
        // Covers: catch (ArgumentException ex) -> return BadRequest(ex.Message);

        // Arrange
        await RegisterAndLoginAsync("bad-type-user");
        var roomId = await CreateRoomAsync("Test Room");

        // Act - Try to create a device with an unknown type (e.g. "Toaster")
        // Service.AddDevice throws ArgumentException for unknown types
        var invalidDeviceRequest = new CreateDeviceRequest("My Toaster", Guid.Parse(roomId), "Toaster");
        var response = await _client.PostAsJsonAsync(DevicesBase, invalidDeviceRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty(); // Verify we got the exception message
    }

    [Fact]
    public async Task AddDevice_ShouldReturnUnauthorized_WhenNotLoggedIn()
    {
        // Covers: catch (UnauthorizedAccessException) -> return Unauthorized();

        // Arrange
        // We DO NOT call RegisterAndLoginAsync here.
        // We use a random GUID for RoomId because Auth check happens before logic validation.
        var request = new CreateDeviceRequest("Secret Lamp", Guid.NewGuid(), "LightBulb");

        // Act
        var response = await _client.PostAsJsonAsync(DevicesBase, request);

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