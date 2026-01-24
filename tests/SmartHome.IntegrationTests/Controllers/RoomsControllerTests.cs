using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class RoomsControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    // Client created by factory by default handles cookies.
    // if we login in first step, client remembers it in next ones.
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateRoom_ShouldAddRoom_WhenUserIsLoggedIn()
    {
        // Arrange (Auth)

        // generate unique email to avoid tests collision
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var email = $"owner-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        // register
        var registerResponse = await _client.PostAsJsonAsync("/api/users/register",
            new RegisterRequest("RoomOwner", email, password));
        registerResponse.EnsureSuccessStatusCode();

        // login - this will set cookie in _client
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login",
            new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();


        var newRoom = new CreateRoomRequest("Salon");
        var createResponse = await _client.PostAsJsonAsync("/api/rooms", newRoom);

        // 201 Created or 200 OK
        createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        // Assert - state verification
        // download list of rooms to chcek if 'Salon' is there
        var getResponse = await _client.GetAsync("/api/rooms");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // use TestDto do read response
        var rooms = await getResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();

        rooms.Should().NotBeNull();
        rooms.Should().Contain(r => r.Name == "Salon");
    }
}

internal record TestRoomDto(string Id, string Name);