using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   An authenticated, or previously authenticated, user of the website.
/// </summary>
public class TwitchUserConfig : ITableModel {
  /// <summary>
  ///   The unique identifier of the configuration.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The user id.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The last timestamp of when the user last updated the configuration.
  /// </summary>
  public DateTime UpdatedOn { get; set; }

  /// <summary>
  ///   Indicates if the user enabled the bot to work on their account.
  /// </summary>
  public bool Enabled { get; set; }

  /// <summary>
  ///   Indicates if the user wants to ban known bots from public lists.
  /// </summary>
  public bool BanKnownBots { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<TwitchUserConfig>(entity => {
      entity.HasKey(e => e.Id);
    });
  }
}