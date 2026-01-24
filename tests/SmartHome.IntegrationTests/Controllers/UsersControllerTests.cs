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

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest("TestUser", "integration@test.com", "Password123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}