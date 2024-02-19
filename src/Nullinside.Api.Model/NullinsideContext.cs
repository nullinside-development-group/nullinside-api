using Microsoft.EntityFrameworkCore;
using Nullinside.Api.Model.Model;

namespace Nullinside.Api.Model;

public class NullinsideContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    protected NullinsideContext()
    {
    }

    public NullinsideContext(DbContextOptions options) : base(options)
    {
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var server = Environment.GetEnvironmentVariable("MYSQL_SERVER");
        var username = Environment.GetEnvironmentVariable("MYSQL_USERNAME");
        var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
        optionsBuilder.UseMySQL($"server={server};database=nullinside;user={username};password={password}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use reflection to get all of the tables we define and call their fake "OnModelCreating" method to setup the
        // database tables and their relationships.
        var databaseTableType = typeof(ITableModel);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => databaseTableType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });
        
        foreach (var type in types)
        {
            var table = Activator.CreateInstance(type) as ITableModel;
            table?.OnModelCreating(modelBuilder);
        }
    }
}