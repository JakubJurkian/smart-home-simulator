using System.Net.Http.Json;
using FluentAssertions;
using Reqnroll;
using SmartHome.Api.Dtos;

namespace SmartHome.BDDTests.Steps;

[Binding]
public class DeviceControlSteps(ScenarioContext scenarioContext, BddTestFactory factory) : IClassFixture<BddTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly ScenarioContext _scenarioContext = scenarioContext;

    [Given(@"I am a registered user named ""(.*)""")]
    public async Task GivenIAmARegisteredUserNamed(string userName)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..4];
        var email = $"{userName.ToLower()}-{uniqueId}@bdd.com";
        var password = "Pass123!";

        var registerRes = await _client.PostAsJsonAsync("/api/users/register",
            new RegisterRequest { Username = userName, Email = email, Password = password });

        if (!registerRes.IsSuccessStatusCode)
        {
            var error = await registerRes.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed: {registerRes.StatusCode} - {error}");
        }

        var loginRes = await _client.PostAsJsonAsync("/api/users/login",
            new LoginRequest { Email = email, Password = password });
        loginRes.EnsureSuccessStatusCode();
    }

    [Given(@"I have a room named ""(.*)""")]
    public async Task GivenIHaveARoomNamed(string roomName)
    {
        var response = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Name = roomName });
        response.EnsureSuccessStatusCode();

        var rooms = await _client.GetFromJsonAsync<List<TestRoomDto>>("/api/rooms");
        var roomId = rooms!.First(r => r.Name == roomName).Id;

        _scenarioContext["CurrentRoomId"] = roomId.ToString();
    }

    [Given(@"I have a device named ""(.*)"" of type ""(.*)"" in ""(.*)""")]
    public async Task GivenIHaveADeviceNamedOfTypeIn(string devName, string type, string roomName)
    {
        var roomId = _scenarioContext.Get<string>("CurrentRoomId");

        var endpoint = type switch
        {
            "LightBulb" => "/api/devices/lightbulb",
            "TemperatureSensor" => "/api/devices/temperaturesensor",
            _ => throw new Exception($"Unknown device type for BDD test: {type}")
        };

        var response = await _client.PostAsJsonAsync(endpoint,
            new CreateDeviceRequest { Name = devName, RoomId = Guid.Parse(roomId), Type = type });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create device at {endpoint}. Status: {response.StatusCode}. Error: {error}");
        }

        var devices = await _client.GetFromJsonAsync<List<DeviceDto>>("/api/devices/all-system");
        
        var device = devices!.FirstOrDefault(d => d.Name == devName);
        
        if (device == null)
        {
             throw new Exception($"Device '{devName}' was created successfully via API but could not be found in the GET list.");
        }

        _scenarioContext[devName] = device.Id.ToString();
    }

    [When(@"I send a request to turn on ""(.*)""")]
    public async Task WhenISendARequestToTurnOn(string devName)
    {
        var deviceId = _scenarioContext.Get<string>(devName);

        var response = await _client.PutAsync($"/api/devices/{deviceId}/turn-on", null);
        
        if (!response.IsSuccessStatusCode)
        {
             var error = await response.Content.ReadAsStringAsync();
             throw new Exception($"Failed to turn on device. Status: {response.StatusCode}. Error: {error}");
        }
    }

    [Then(@"The device ""(.*)"" should be ON")]
    public async Task ThenTheDeviceShouldBeON(string devName)
    {
        var deviceId = _scenarioContext.Get<string>(devName);

        var device = await _client.GetFromJsonAsync<DeviceDto>($"/api/devices/{deviceId}");

        device.Should().NotBeNull();
        device!.IsOn.Should().BeTrue();
    }

    internal record TestRoomDto(Guid Id, string Name);
}