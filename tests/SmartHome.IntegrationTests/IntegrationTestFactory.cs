using System.Data.Common;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization; // Added for AuthorizationPolicy
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartHome.Infrastructure.Persistence;
using SmartHome.Domain.Entities;

namespace SmartHome.IntegrationTests;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Force environment to "Testing"
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 2. Clear existing database registrations
            services.RemoveAll<DbContextOptions<SmartHomeDbContext>>();
            services.RemoveAll<DbConnection>();

            // 3. Set up SQLite In-Memory
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<SmartHomeDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseInternalServiceProvider(null);
            });

            // 4. Force Authentication and Authorization to use our Mock Scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
                options.DefaultScheme = "TestScheme";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            // Ensure the authorization policy also points to our TestScheme
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("TestScheme")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // 5. Remove real background tasks to avoid port conflicts
            services.RemoveAll<IHostedService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
            db.Database.EnsureCreated();

            // Seed Test User
            if (!db.Users.Any(u => u.Id == TestConstants.TestUserId))
            {
                db.Users.Add(new User 
                { 
                    Id = TestConstants.TestUserId, 
                    Username = "TestUser", 
                    Email = "test@integration.com",
                    PasswordHash = "MockHash"
                });
                db.SaveChanges();
            }
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}

public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Define claims for the mock user
        var claims = new[] {
            new Claim(ClaimTypes.Name, "TestUser"),
            // Ensure this matches exactly what your controller's GetCurrentUserId expects
            new Claim(ClaimTypes.NameIdentifier, TestConstants.TestUserId.ToString()),
            new Claim("sub", TestConstants.TestUserId.ToString()) // Some systems prefer 'sub'
        };

        // Create identity and principal
        var identity = new ClaimsIdentity(claims, "TestAuthType"); // "TestAuthType" makes IsAuthenticated = true
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class TestConstants
{
    public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}