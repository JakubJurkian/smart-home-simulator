using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Entities;

namespace SmartHome.Infrastructure.Persistence;

public class SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : DbContext(options)
{
    // DbSet represents a "Table" in the database.
    // Specifically, the "Devices" table which will store objects of type Device.
    public DbSet<Device> Devices { get; set; }
    public DbSet<User> Users { get; set; }

    // This method runs on startup to configure the Object-Relational Mapping (ORM).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Inheritance Configuration (TPH - Table Per Hierarchy).
        // EF Core will by default put LightBulbs and Sensors into a SINGLE "Devices" table.
        // It adds a "Discriminator" column to distinguish between types.

        modelBuilder.Entity<Device>()
            .HasKey(d => d.Id); // Defines 'Id' as the Primary Key
        // Registration of derived types (children).
        // Without this, EF Core won't know these classes exist as part of the 'Device' family.
        modelBuilder.Entity<LightBulb>();
        modelBuilder.Entity<TemperatureSensor>();

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        base.OnModelCreating(modelBuilder);
        // Even if the base class does nothing here currently, future EF Core versions 
        // might add default configurations. Calling 'base' ensures we extend the logic 
        // rather than replacing or removing it.
    }
}