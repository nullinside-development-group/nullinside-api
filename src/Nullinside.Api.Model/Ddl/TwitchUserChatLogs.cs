using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   A user chat log.
/// </summary>
public class TwitchUserChatLogs : ITableModel {
  /// <summary>
  ///   The unique identifier of the log.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The channel the chat happened in.
  /// </summary>
  public string? Channel { get; set; }

  /// <summary>
  ///   The id of the user that chatted.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public string? TwitchId { get; set; }

  /// <summary>
  ///   The username of the user that chatted.
  /// </summary>
  public string? TwitchUsername { get; set; }

  /// <summary>
  ///   The message sent.
  /// </summary>
  public string? Message { get; set; }

  /// <summary>
  ///   The timestamp of when the message was sent.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchUserChatLogs>(entity => {
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