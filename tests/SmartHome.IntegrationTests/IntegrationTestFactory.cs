using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.IntegrationTests;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<SmartHomeDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(SmartHomeDbContext));
            services.RemoveAll(typeof(DbConnection));

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            services.AddSingleton<DbConnection>(_ => _connection);
            services.AddDbContext<SmartHomeDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}