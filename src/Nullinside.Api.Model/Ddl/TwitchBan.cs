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
  public User Channel { get; set; }

  /// <summary>
  ///   The user that was banned.
  /// </summary>
  /// <remarks>Don't ask me why they made this a string.</remarks>
  public TwitchUser BannedUser { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchBan>(entity => {
      entity.HasKey(e => e.Id);
      entity.HasOne(e => e.Channel);
      entity.HasOne(e => e.BannedUser);
    });
  }
}