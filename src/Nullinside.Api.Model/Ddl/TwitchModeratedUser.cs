using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   A channel we are a moderator of.
/// </summary>
public class TwitchModeratedUser : ITableModel {
  /// <summary>
  ///   The unique identifier of a channel we moderate for.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The identifier of the channel we moderate.
  /// </summary>
  public string ChannelId { get; set; } = null!;
  
  /// <summary>
  ///   The name of the channel we moderate for display purposes.
  /// </summary>
  public string ChannelName { get; set; } = null!;

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchModeratedUser>(entity => {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.ChannelId)
        .HasMaxLength(255);
      entity.Property(e => e.ChannelName)
        .HasMaxLength(255);
    });
  }
}