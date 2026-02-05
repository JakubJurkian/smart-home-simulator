using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartHome.Api.Dtos;
using Xunit;

namespace SmartHome.IntegrationTests.Controllers;

// IClassFixture creates factory (API) in memory ONCE for all tests in this class
public class UsersControllerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private const string UsersBase = "/api/users";

    #region Register & Login (Existing Tests)

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest("TestUser", "integration@test.com", "Password123!");

        // Act
        var response = await _client.PostAsJsonAsync($"{UsersBase}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_ShouldReturnOkAndSetCookie_WhenCredentialsAreValid()
    {
        var registerRequest = new RegisterRequest("CookieUser", "cookie@test.com", "Password123!");
        await _client.PostAsJsonAsync($"{UsersBase}/register", registerRequest);

        var loginRequest = new LoginRequest("cookie@test.com", "Password123!");

        var response = await _client.PostAsJsonAsync($"{UsersBase}/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.Contains("Set-Cookie").Should().BeTrue();

        var cookieHeader = response.Headers.GetValues("Set-Cookie").First();
        cookieHeader.Should().Contain("userId");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Login successful!");
        content.Should().Contain("cookie@test.com");
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenUserIsLoggedIn()
    {
        // Arrange
        await RegisterAndLoginAsync("logout-tester");

        // Act
        var response = await _client.PostAsync($"{UsersBase}/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Update (PUT)

    [Fact]
    public async Task UpdateUser_ShouldSucceed_WhenUpdatingOwnProfile()
    {
        // Arrange - Create user and login
        var (userId, _) = await RegisterAndLoginAsync("updater");

        var updateRequest = new UpdateUserRequest("UpdatedName", "NewPass123!");

        // Act - Update own profile
        var response = await _client.PutAsJsonAsync($"{UsersBase}/{userId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn403_WhenUpdatingSomeoneElse()
    {
        // Security Test: Attacker tries to update Victim's account

        // Arrange
        // Register Victim (we need their ID)
        var (victimId, _) = await RegisterUserAsync("victim-update");

        // Register and Login as Attacker
        await RegisterAndLoginAsync("attacker-update");

        var updateRequest = new UpdateUserRequest("HackedName", "HackedPass!");

        // Act - Attacker hits Victim's endpoint
        var response = await _client.PutAsJsonAsync($"{UsersBase}/{victimId}", updateRequest);

        // Assert - Should be Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturn401_WhenNotLoggedIn()
    {
        // Sad Path: No Auth cookie
        var randomId = Guid.NewGuid();
        var updateRequest = new UpdateUserRequest("Ghost", "Pass");

        var response = await _client.PutAsJsonAsync($"{UsersBase}/{randomId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Delete (DELETE)

    [Fact]
    public async Task DeleteUser_ShouldReturn204_WhenDeletingOwnAccount()
    {
        // Arrange
        var (userId, _) = await RegisterAndLoginAsync("delete-me");

        // Act
        var response = await _client.DeleteAsync($"{UsersBase}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify: Try to access protected resource (like logout or update) - should fail or be 401
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn403_WhenDeletingSomeoneElse()
    {
        // Security Test: Attacker tries to delete Victim

        // Arrange
        var (victimId, _) = await RegisterUserAsync("victim-delete");
        await RegisterAndLoginAsync("attacker-delete");

        // Act
        var response = await _client.DeleteAsync($"{UsersBase}/{victimId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn401_WhenNotLoggedIn()
    {
        var response = await _client.DeleteAsync($"{UsersBase}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    // Helper to Register, Login, and return the User ID and Email
    private async Task<(Guid, string)> RegisterAndLoginAsync(string prefix)
    {
        var (id, email) = await RegisterUserAsync(prefix);

        var loginRes = await _client.PostAsJsonAsync($"{UsersBase}/login", new LoginRequest(email, "Password123!"));
        loginRes.EnsureSuccessStatusCode();

        return (id, email);
    }

    // Helper to just Register and return ID (without logging in)
    private async Task<(Guid, string)> RegisterUserAsync(string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..6];
        var email = $"{prefix}-{unique}@test.com";
        var password = "Password123!";

        var response = await _client.PostAsJsonAsync($"{UsersBase}/register", new RegisterRequest(prefix, email, password));
        response.EnsureSuccessStatusCode();

        try
        {
            var result = await response.Content.ReadFromJsonAsync<TestUserResponse>();
            return (result!.Id, email);
        }
        catch
        {
            return (Guid.Empty, email);
        }
    }

    #endregion
}

// DTOs for Tests
internal record UpdateUserRequest(string Username, string Password);
internal record TestUserResponse(Guid Id, string Email, string Username);