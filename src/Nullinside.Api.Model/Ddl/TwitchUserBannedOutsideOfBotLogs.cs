using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   A log of users banned by someone other than the bot.
/// </summary>
public class TwitchUserBannedOutsideOfBotLogs : ITableModel {
  /// <summary>
  ///   The unique identifier of the ban.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The channel the ban happened in.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public string? Channel { get; set; }

  /// <summary>
  ///   The id of the user that was banned.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public string? TwitchId { get; set; }

  /// <summary>
  ///   The username of the user that was banned.
  /// </summary>
  public string? TwitchUsername { get; set; }

  /// <summary>
  ///   The reason for the ban.
  /// </summary>
  public string? Reason { get; set; }

  /// <summary>
  ///   The timestamp of when the ban occured.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchUserBannedOutsideOfBotLogs>(entity => {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.TwitchId)
        .HasMaxLength(255);
      entity.Property(e => e.TwitchUsername)
        .HasMaxLength(255);
      entity.Property(e => e.Channel)
        .HasMaxLength(255);
    });
  }
}