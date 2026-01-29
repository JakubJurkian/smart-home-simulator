using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

// CONFIGURATION
using var httpClient = new HttpClient();
var baseHtmlUrl = "http://localhost:5187"; 

// SETUP (Login to get Cookie)
Console.WriteLine("Logging in to get authentication cookie...");
var loginContent = new StringContent(
    JsonSerializer.Serialize(new { Email = "admin@test.com", Password = "Pass123!" }),
    Encoding.UTF8,
    "application/json");

// Register first
try {
    await httpClient.PostAsync($"{baseHtmlUrl}/api/users/register", 
        new StringContent(JsonSerializer.Serialize(new { Username = "Admin", Email = "admin@test.com", Password = "Pass123!" }), Encoding.UTF8, "application/json"));
} catch {}

// Login
var loginResponse = await httpClient.PostAsync($"{baseHtmlUrl}/api/users/login", loginContent);

if (!loginResponse.IsSuccessStatusCode)
{
    Console.WriteLine($"Login failed! Status: {loginResponse.StatusCode}. Cannot proceed.");
    return;
}

Console.WriteLine("Login successful! Starting Load Test...");

// DEFINE SCENARIO
var scenario = Scenario.Create("fetch_rooms_load_test", async context =>
{
    var request = Http.CreateRequest("GET", $"{baseHtmlUrl}/api/rooms")
        .WithHeader("Accept", "application/json");

    var response = await Http.Send(httpClient, request);
    return response;
})
.WithoutWarmUp()
.WithLoadSimulations(
    /// 50 USERS FOR 30 SECONDS
    Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
);

// RUN
NBomberRunner
    .RegisterScenarios(scenario)
    .Run();