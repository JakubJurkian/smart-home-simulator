using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class RoomsControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = true
    });
    private const string RoomsBase = "/api/rooms";
    private const string UsersRegister = "/api/users/register";
    private const string UsersLogin = "/api/users/login";

    private async Task RegisterAndLoginUserAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var email = $"owner-{uniqueSuffix}@test.com";
        var password = "Pass123!";

        // Register
        var registerResponse = await _client.PostAsJsonAsync(UsersRegister,
            new RegisterRequest { Username = "RoomOwner", Email = email, Password = password });
        registerResponse.EnsureSuccessStatusCode();

        // Login
        var loginResponse = await _client.PostAsJsonAsync(UsersLogin,
            new LoginRequest { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
    }

    #region CreateRoom Tests

    [Fact]
    public async Task CreateRoom_ShouldAddRoom_WhenUserIsLoggedIn()
    {
        // Arrange
        await RegisterAndLoginUserAsync();
        var newRoom = new CreateRoomRequest { Name = "Salon" };

        // Act
        var createResponse = await _client.PostAsJsonAsync(RoomsBase, newRoom);

        // Assert
        createResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync(RoomsBase);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rooms = await getResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        rooms.Should().NotBeNull();
        rooms.Should().Contain(r => r.Name == "Salon");
    }

    [Fact]
    public async Task CreateRoom_ShouldFail_WhenUserIsNotLoggedIn()
    {
        // Arrange - no login
        var newRoom = new CreateRoomRequest { Name = "Kitchen" };

        // Act
        var createResponse = await _client.PostAsJsonAsync(RoomsBase, newRoom);

        // Assert
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
        var response1 = await _client.PostAsJsonAsync(RoomsBase, new CreateRoomRequest { Name = "Bedroom" });
        var response2 = await _client.PostAsJsonAsync(RoomsBase, new CreateRoomRequest { Name = "Bathroom" });

        // Assert
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var getResponse = await _client.GetAsync(RoomsBase);
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

        // Create initial room
        await _client.PostAsJsonAsync(RoomsBase, new CreateRoomRequest { Name = "OldName" });

        // Get Room ID
        var getResponse = await _client.GetAsync(RoomsBase);
        var rooms = await getResponse.Content.ReadFromJsonAsync<List<TestRoomDto>>();
        var roomId = rooms!.First(r => r.Name == "OldName").Id;

        // Act
        // We use an anonymous object that matches the JSON structure expected by RenameRoomRequest
        var renameRequest = new { Name = "NewName" };
        var renameResponse = await _client.PutAsJsonAsync($"{RoomsBase}/{roomId}", renameRequest);

        // Assert
        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRooms = await (await _client.GetAsync(RoomsBase))
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
        var renameRequest = new { Name = "NewName" };

        // Act
        var renameResponse = await _client.PutAsJsonAsync($"{RoomsBase}/{nonExistentId}", renameRequest);

        // Assert
        renameResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameRoom_ShouldFail_WhenUserIsNotLoggedIn()
    {
        // Arrange - no login
        var roomId = Guid.NewGuid();
        var renameRequest = new { Name = "NewName" };

        // Act
        var renameResponse = await _client.PutAsJsonAsync($"{RoomsBase}/{roomId}", renameRequest);

        // Assert
        renameResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region DeleteRoom Tests

    [Fact]
    public async Task DeleteRoom_ShouldSucceed_WhenRoomExists()
    {
        // Arrange
        await RegisterAndLoginUserAsync();
        await _client.PostAsJsonAsync(RoomsBase, new CreateRoomRequest { Name = "RoomToDelete" });

        var rooms = await _client.GetFromJsonAsync<List<TestRoomDto>>(RoomsBase);
        var roomId = rooms!.First(r => r.Name == "RoomToDelete").Id;

        // Act
        var deleteResponse = await _client.DeleteAsync($"{RoomsBase}/{roomId}");

        // Assert
        deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var checkRooms = await _client.GetFromJsonAsync<List<TestRoomDto>>(RoomsBase);
        checkRooms.Should().NotContain(r => r.Name == "RoomToDelete");
    }

    #endregion
}

// Local DTOs for testing
internal record TestRoomDto(string Id, string Name);