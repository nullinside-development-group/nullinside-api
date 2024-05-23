using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   An authenticated, or previously authenticated, user of the website.
/// </summary>
public class FeatureToggle : ITableModel {
  /// <summary>
  ///   The unique identifier of the feature.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The string name of the feature.
  /// </summary>
  public string? Feature { get; set; }

  /// <summary>
  ///   True if the feature is enabled, false otherwise.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<FeatureToggle>(entity => {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Feature)
        .HasMaxLength(255);
    });
  }
}