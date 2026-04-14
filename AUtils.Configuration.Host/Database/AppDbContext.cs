using Microsoft.EntityFrameworkCore;

namespace AUtils.Configuration.Host.Database;

/// <summary>
/// Database application context
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Users
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Configurations
    /// </summary>
    public DbSet<Configuration> Configurations { get; set; }
    
    /// <summary>
    /// .ctor
    /// </summary>
    public AppDbContext() {}
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="options"></param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    
    /// <summary>
    /// Build ORM model
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Configuration>()
            .HasKey(x => new {x.Role, x.Environment});
    }
}