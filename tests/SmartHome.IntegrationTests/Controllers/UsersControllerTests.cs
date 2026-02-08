using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

public class UsersControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private const string UsersBase = "/api/users";

    #region Register & Login

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest { Username = "TestUser", Email = "integration@test.com", Password = "Password123!" };

        // Act
        var response = await _client.PostAsJsonAsync($"{UsersBase}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_ShouldReturnOkAndSetCookie_WhenCredentialsAreValid()
    {
        var registerRequest = new RegisterRequest { Username = "CookieUser", Email = "cookie@test.com", Password = "Password123!" };
        await _client.PostAsJsonAsync($"{UsersBase}/register", registerRequest);

        var loginRequest = new LoginRequest { Email = "cookie@test.com", Password = "Password123!" };

        var response = await _client.PostAsJsonAsync($"{UsersBase}/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Set-Cookie").Should().BeTrue();

        var cookieHeader = response.Headers.GetValues("Set-Cookie").First();
        cookieHeader.Should().Contain("userId");
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenUserIsLoggedIn()
    {
        await RegisterAndLoginAsync("logout-tester");

        var response = await _client.PostAsync($"{UsersBase}/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Update (PUT)

    [Fact]
    public async Task UpdateUser_ShouldSucceed_WhenUpdatingOwnProfile()
    {
        var (userId, _) = await RegisterAndLoginAsync("updater");
        var updateRequest = new UpdateUserRequest { Username = "UpdatedName", Password = "NewPass123!" };

        var response = await _client.PutAsJsonAsync($"{UsersBase}/{userId}", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn403_WhenUpdatingSomeoneElse()
    {
        var (victimId, _) = await RegisterUserAsync("victim-up");
        
        await RegisterAndLoginAsync("attacker-up");

        var updateRequest = new UpdateUserRequest { Username = "Hacked", Password = "HackedPass!" };
        
        var response = await _client.PutAsJsonAsync($"{UsersBase}/{victimId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn401_WhenNotLoggedIn()
    {
        var randomId = Guid.NewGuid();
        var updateRequest = new UpdateUserRequest { Username = "Ghost", Password = "Pass" };

        var response = await _client.PutAsJsonAsync($"{UsersBase}/{randomId}", updateRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Delete (DELETE)

    [Fact]
    public async Task DeleteUser_ShouldReturn204_WhenDeletingOwnAccount()
    {
        var (userId, _) = await RegisterAndLoginAsync("delete-me");
        
        var response = await _client.DeleteAsync($"{UsersBase}/{userId}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn403_WhenDeletingSomeoneElse()
    {
        var (victimId, _) = await RegisterUserAsync("victim-del");
        await RegisterAndLoginAsync("attacker-del");

        var response = await _client.DeleteAsync($"{UsersBase}/{victimId}");
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn401_WhenNotLoggedIn()
    {
        var response = await _client.DeleteAsync($"{UsersBase}/{Guid.NewGuid()}");
        
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helpers

    private async Task<(Guid, string)> RegisterAndLoginAsync(string prefix)
    {
        var (id, email) = await RegisterUserAsync(prefix);
        var loginRes = await _client.PostAsJsonAsync($"{UsersBase}/login", new LoginRequest { Email = email, Password = "Password123!" });
        loginRes.EnsureSuccessStatusCode();
        return (id, email);
    }

    private async Task<(Guid, string)> RegisterUserAsync(string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..6];
        var email = $"{prefix}-{unique}@test.com";
        var password = "Password123!";

        var response = await _client.PostAsJsonAsync($"{UsersBase}/register", 
            new RegisterRequest { Username = prefix, Email = email, Password = password });
        
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        if (result.TryGetProperty("id", out var idElement) && idElement.GetString() is string idStr)
        {
            return (Guid.Parse(idStr), email);
        }
        
        throw new Exception("Failed to retrieve ID from registration response");
    }

    #endregion
}

// Lokalne DTO
internal record UpdateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string? Password { get; init; }
}