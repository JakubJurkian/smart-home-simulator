using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Reqnroll;
using SmartHome.Api.Dtos;

namespace SmartHome.BDDTests.Steps;

[Binding]
public class DeviceControlSteps : IClassFixture<BddTestFactory>
{
    private readonly HttpClient _client;
    private readonly ScenarioContext _scenarioContext;

    public DeviceControlSteps(ScenarioContext scenarioContext, BddTestFactory factory)
    {
        _scenarioContext = scenarioContext;
        _client = factory.CreateClient();
    }

    [Given(@"I am a registered user named ""(.*)""")]
    public async Task GivenIAmARegisteredUserNamed(string userName)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..4];
        var email = $"{userName.ToLower()}-{uniqueId}@bdd.com";
        var password = "Pass123!";

        var registerRes = await _client.PostAsJsonAsync("/api/users/register",
            new RegisterRequest(userName, email, password));

        if (!registerRes.IsSuccessStatusCode)
        {
            var error = await registerRes.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed: {registerRes.StatusCode} - {error}");
        }

        var loginRes = await _client.PostAsJsonAsync("/api/users/login",
            new LoginRequest(email, password));
        loginRes.EnsureSuccessStatusCode();
    }

    [Given(@"I have a room named ""(.*)""")]
    public async Task GivenIHaveARoomNamed(string roomName)
    {
        await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest(roomName));

        var rooms = await _client.GetFromJsonAsync<List<TestRoomDto>>("/api/rooms");
        var roomId = rooms!.First(r => r.Name == roomName).Id;

        _scenarioContext["CurrentRoomId"] = roomId.ToString();
    }

    [Given(@"I have a device named ""(.*)"" of type ""(.*)"" in ""(.*)""")]
    public async Task GivenIHaveADeviceNamedOfTypeIn(string devName, string type, string roomName)
    {
        var roomId = _scenarioContext.Get<string>("CurrentRoomId");

        await _client.PostAsJsonAsync("/api/devices",
            new CreateDeviceRequest(devName, Guid.Parse(roomId), type));

        var devices = await _client.GetFromJsonAsync<List<DeviceDto>>("/api/devices");
        var deviceId = devices!.First(d => d.Name == devName).Id;

        _scenarioContext[devName] = deviceId.ToString();
    }

    [When(@"I send a request to turn on ""(.*)""")]
    public async Task WhenISendARequestToTurnOn(string devName)
    {
        var deviceId = _scenarioContext.Get<string>(devName);

        var response = await _client.PutAsync($"/api/devices/{deviceId}/turn-on", null);
        response.EnsureSuccessStatusCode();
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