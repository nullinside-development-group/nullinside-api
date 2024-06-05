using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   An authenticated, or previously authenticated, user of the website.
/// </summary>
public class TwitchUser : ITableModel {
  /// <summary>
  ///   The unique identifier of the user.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The id of the user on twitch.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public string? TwitchId { get; set; }

  /// <summary>
  ///   The username of the user on twitch.
  /// </summary>
  public string? TwitchUsername { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchUser>(entity => {
      entity.HasKey(e => e.Id);
      entity.HasIndex(e => e.TwitchId)
        .IsUnique();
      entity.Property(e => e.TwitchId)
        .HasMaxLength(255);
      entity.Property(e => e.TwitchUsername)
        .HasMaxLength(255);
    });
  }
}