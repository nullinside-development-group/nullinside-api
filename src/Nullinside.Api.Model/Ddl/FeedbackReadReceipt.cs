using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   Represents that a user has read a piece of feedback at least once.
/// </summary>
public class FeedbackReadReceipt : ITableModel {
  /// <summary>
  ///   Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The nullinside database user id of the user who submitted the feedback.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The feedback that was read.
  /// </summary>
  public int FeedbackId { get; set; }

  /// <summary>
  ///   The timestamp of when the feedback was read.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The user object associated with this.
  /// </summary>
  public User User { get; set; } = null!;

  /// <summary>
  ///   The feedback object associated with this.
  /// </summary>
  public Feedback Feedback { get; set; } = null!;

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<FeedbackReadReceipt>(entity => {
      entity.HasOne(e => e.User)
        .WithMany(e => e.FeedbackReadReceipts)
        .HasForeignKey(e => e.UserId)
        .HasPrincipalKey(e => e.Id);

      entity.HasOne(e => e.Feedback)
        .WithMany(e => e.FeedbackReadReceipts)
        .HasForeignKey(e => e.FeedbackId)
        .HasPrincipalKey(e => e.Id);
    });
  }
}