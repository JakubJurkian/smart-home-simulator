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
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SmartHomeDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            var dbContextServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(SmartHomeDbContext));
            if (dbContextServiceDescriptor != null) services.Remove(dbContextServiceDescriptor);

            services.RemoveAll<DbConnection>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<SmartHomeDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseInternalServiceProvider(null);
            });
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
        }

        return host;
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