using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartHome.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;


namespace SmartHome.IntegrationTests;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        return host;
    }
}