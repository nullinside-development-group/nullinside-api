using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model.Model;

namespace Nullinside.Api.Model;

public class NullinsideContext : DbContext {
  protected NullinsideContext() {
  }

  public NullinsideContext(DbContextOptions options) : base(options) {
  }

  public DbSet<User> Users { get; set; } = null!;
  public DbSet<UserRole> UserRoles { get; set; } = null!;

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
    string? server = Environment.GetEnvironmentVariable("MYSQL_SERVER");
    string? username = Environment.GetEnvironmentVariable("MYSQL_USERNAME");
    string? password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
    optionsBuilder.UseMySQL($"server={server};database=nullinside;user={username};password={password}");
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);

    // Use reflection to get all of the tables we define and call their fake "OnModelCreating" method to setup the
    // database tables and their relationships.
    Type databaseTableType = typeof(ITableModel);
    IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(s => s.GetTypes())
      .Where(p => databaseTableType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });

    foreach (Type type in types) {
      ITableModel? table = Activator.CreateInstance(type) as ITableModel;
      table?.OnModelCreating(modelBuilder);
    }
  }
}