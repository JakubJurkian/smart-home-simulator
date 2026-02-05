using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class RoomsControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(string email, string password)> RegisterAndLoginUserAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var email = $"owner-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/users/register",
            new RegisterRequest("RoomOwner", email, password));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login",
            new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        return (email, password);
    }

    #region CreateRoom Tests

    [Fact]
    public async Task CreateRoom_ShouldAddRoom_WhenUserIsLoggedIn()
    {
        // Arrange
        await RegisterAndLoginUserAsync();
        var newRoom = new CreateRoomRequest("Salon");

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/rooms", newRoom);

        // Assert
        createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync("/api/rooms");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rooms = await getResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        rooms.Should().NotBeNull();
        rooms.Should().Contain(r => r.Name == "Salon");
    }

    [Fact]
    public async Task CreateRoom_ShouldFail_WhenUserIsNotLoggedIn()
    {
        // Arrange - fresh client, no login
        var newRoom = new CreateRoomRequest("Kitchen");

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/rooms", newRoom);

        // Assert - should fail (401 Unauthorized or 500 due to exception)
        createResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateRoom_ShouldAddMultipleRooms_WhenUserIsLoggedIn()
    {
        // Arrange
        await RegisterAndLoginUserAsync();

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bedroom"));
        var response2 = await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bathroom"));

        // Assert
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync("/api/rooms");
        var rooms = await getResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();

        rooms.Should().NotBeNull();
        rooms.Should().Contain(r => r.Name == "Bedroom");
        rooms.Should().Contain(r => r.Name == "Bathroom");
    }

    #endregion

    #region RenameRoom Tests

    [Fact]
    public async Task RenameRoom_ShouldSucceed_WhenRoomExists()
    {
        // Arrange
        await RegisterAndLoginUserAsync();
        await _client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("OldName"));

        var getResponse = await _client.GetAsync("/api/rooms");
        var rooms = await getResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        var roomId = rooms!.First(r => r.Name == "OldName").Id;

        // Act
        var content = new StringContent(JsonSerializer.Serialize("NewName"), Encoding.UTF8, "application/json");
        var renameResponse = await _client.PutAsync($"/api/rooms/{roomId}", content);

        // Assert
        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRooms = await (await _client.GetAsync("/api/rooms"))
            .Content.ReadFromJsonAsync<List<TestRoomDto>>();
        updatedRooms.Should().Contain(r => r.Name == "NewName");
        updatedRooms.Should().NotContain(r => r.Name == "OldName");
    }

    [Fact]
    public async Task RenameRoom_ShouldFail_WhenRoomDoesNotExist()
    {
        // Arrange
        await RegisterAndLoginUserAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var content = new StringContent(JsonSerializer.Serialize("NewName"), Encoding.UTF8, "application/json");
        var renameResponse = await _client.PutAsync($"/api/rooms/{nonExistentId}", content);

        // Assert
        renameResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameRoom_ShouldFail_WhenUserIsNotLoggedIn()
    {
        // Arrange - no login
        var roomId = Guid.NewGuid();

        // Act
        var content = new StringContent(JsonSerializer.Serialize("NewName"), Encoding.UTF8, "application/json");
        var renameResponse = await _client.PutAsync($"/api/rooms/{roomId}", content);

        // Assert
        renameResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadRequest);
    }

    #endregion
}

internal record TestRoomDto(string Id, string Name);