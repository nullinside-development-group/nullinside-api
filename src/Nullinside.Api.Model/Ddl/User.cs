using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   An authenticated, or previously authenticated, user of the website.
/// </summary>
public class User : ITableModel {
  /// <summary>
  ///   The unique identifier of the user.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The user's email account.
  /// </summary>
  public string? Email { get; set; }

  /// <summary>
  ///   The user's auth token for interacting with the site's API.
  /// </summary>
  public string? Token { get; set; }

  /// <summary>
  ///   The oauth token for interacting with the twitch API.
  /// </summary>
  public string? TwitchToken { get; set; }

  /// <summary>
  ///   The refresh token for interacting with the twitch API.
  /// </summary>
  public string? TwitchRefreshToken { get; set; }

  /// <summary>
  ///   When the <see cref="TwitchToken" /> expires.
  /// </summary>
  public DateTime? TwitchTokenExpiration { get; set; }

  /// <summary>
  ///   The last timestamp of when the user logged into the site.
  /// </summary>
  public DateTime UpdatedOn { get; set; }

  /// <summary>
  ///   The last timestamp of when the user created their account.
  /// </summary>
  public DateTime CreatedOn { get; set; }

  /// <summary>
  ///   The roles the user has in the application, what they have access to.
  /// </summary>
  public IEnumerable<UserRole>? Roles { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<User>(entity => {
      entity.HasKey(e => e.Id);
      entity.HasIndex(e => e.Email)
        .IsUnique();
      entity.HasMany(e => e.Roles);
      entity.Property(e => e.Email)
        .HasMaxLength(255);
      entity.Property(e => e.Token)
        .HasMaxLength(255);
    });
  }
}