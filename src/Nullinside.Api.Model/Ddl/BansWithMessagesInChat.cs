using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   The list of messages someone sent before they were banned.
/// </summary>
/// <remarks>
///   The view was created as part of 20240618190138_AddBansWithMessagesView.
/// </remarks>
public class BansWithMessagesInChat : ITableModel {
  /// <summary>
  ///   Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The channel the message was sent in.
  /// </summary>
  public string Channel { get; set; } = null!;

  /// <summary>
  ///   The username of the user that sent the message.
  /// </summary>
  public string TwitchUsername { get; set; } = null!;

  /// <summary>
  ///   The message that was sent.
  /// </summary>
  public string Message { get; set; } = null!;

  /// <summary>
  ///   The timestamp of when the message was sent.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<BansWithMessagesInChat>(entity => {
      entity.HasNoKey();

      entity.ToView("BansWithMessagesInChat");

      entity.Property(e => e.Id);
      entity.Property(e => e.Channel);
      entity.Property(e => e.TwitchUsername);
      entity.Property(e => e.Message);
      entity.Property(e => e.Timestamp);
    });
  }
}