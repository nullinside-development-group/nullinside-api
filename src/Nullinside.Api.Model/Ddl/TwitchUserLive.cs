using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   A twitch user of the bot that is currently live.
/// </summary>
public class TwitchUserLive : ITableModel {
  /// <summary>
  ///   The unique identifier of the entry.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The user id.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The total view count for the stream.
  /// </summary>
  public int ViewerCount { get; set; }

  /// <summary>
  ///   The time the stream went live.
  /// </summary>
  public DateTime GoneLiveTime { get; set; }

  /// <summary>
  ///   The title of the stream.
  /// </summary>
  public string? StreamTitle { get; set; }

  /// <summary>
  ///   The name of the game being played on the stream.
  /// </summary>
  public string? GameName { get; set; }

  /// <summary>
  ///   The url of the twitch generated thumbnail for the stream.
  /// </summary>
  public string? ThumbnailUrl { get; set; }

  /// <summary>
  ///   The user object associated with this.
  /// </summary>
  public User User { get; set; } = null!;

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchUserLive>(entity => {
      entity.HasOne(e => e.User)
        .WithOne(e => e.TwitchUserLive)
        .HasForeignKey<TwitchUserLive>(e => e.UserId)
        .HasPrincipalKey<User>(e => e.Id);

      entity.Property(e => e.StreamTitle)
        .HasMaxLength(140); // From obs

      entity.Property(e => e.GameName)
        .HasMaxLength(255);

      entity.Property(e => e.ThumbnailUrl)
        .HasMaxLength(2048);
    });
  }
}