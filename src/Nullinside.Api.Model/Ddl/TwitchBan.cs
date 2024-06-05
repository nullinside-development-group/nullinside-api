using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   An authenticated, or previously authenticated, user of the website.
/// </summary>
public class TwitchBan : ITableModel {
  /// <summary>
  ///   The unique identifier of the user.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The channel they were banned in.
  /// </summary>
  public string ChannelId { get; set; } = null!;

  /// <summary>
  ///   The user that was banned.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public string BannedUserTwitchId { get; set; } = null!;

  /// <summary>
  ///   The reason for the ban.
  /// </summary>
  public string Reason { get; set; } = null!;

  /// <summary>
  ///   The timestamp of when the ban was performed.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchBan>(entity => {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.ChannelId)
        .HasMaxLength(255);
      entity.Property(e => e.BannedUserTwitchId)
        .HasMaxLength(255);
      entity.Property(e => e.Reason)
        .HasMaxLength(255);
    });
  }
}