using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Model;

/// <summary>
///   The nullinside database.
/// </summary>
public class NullinsideContext : DbContext {
  /// <summary>
  ///   Initializes a new instance of <see cref="NullinsideContext" />
  /// </summary>
  protected NullinsideContext() {
  }

  /// <summary>
  ///   Initializes a new instance of <see cref="NullinsideContext" />
  /// </summary>
  /// <param name="options">The options for configuring the database connection.</param>
  public NullinsideContext(DbContextOptions<NullinsideContext> options) : base(options) {
  }

  /// <summary>
  ///   The users table which contains all of the users that have ever authenticated with the site.
  /// </summary>
  public DbSet<User> Users { get; set; } = null!;

  /// <summary>
  ///   The user's roles table which contains all of the "roles" the user has in the application.
  /// </summary>
  public DbSet<UserRole> UserRoles { get; set; } = null!;

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  public DbSet<DockerDeployments> DockerDeployments { get; set; } = null!;

  /// <summary>
  ///   Configures the default database connection.
  /// </summary>
  /// <param name="optionsBuilder">The database configuration options.</param>
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
    string? server = Environment.GetEnvironmentVariable("MYSQL_SERVER");
    string? username = Environment.GetEnvironmentVariable("MYSQL_USERNAME");
    string? password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
    optionsBuilder.UseMySQL($"server={server};database=nullinside;user={username};password={password};AllowUserVariables=true");
  }

  /// <summary>
  ///   Dynamically finds all <seealso cref="ITableModel" /> classes and generates tables from their definitions.
  /// </summary>
  /// <param name="modelBuilder">The model builder passed to us by the framework.</param>
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    base.OnModelCreating(modelBuilder);

    // Use reflection to get all of the tables we define and call their fake "OnModelCreating" method to setup the
    // database tables and their relationships.
    Type databaseTableType = typeof(ITableModel);
    IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(s => s.GetTypes())
      .Where(p => databaseTableType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });

    foreach (Type type in types) {
      var table = Activator.CreateInstance(type) as ITableModel;
      table?.OnModelCreating(modelBuilder);
    }
  }
}