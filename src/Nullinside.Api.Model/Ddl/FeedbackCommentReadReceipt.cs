using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   Represents that a user has read a comment.
/// </summary>
public class FeedbackCommentReadReceipt : ITableModel {
  /// <summary>
  ///   Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The nullinside database user id of the user who submitted the feedback.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The feedback comment that was read.
  /// </summary>
  public int FeedbackCommentId { get; set; }

  /// <summary>
  ///   The timestamp of when the feedback comment was read.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The user object associated with this.
  /// </summary>
  public User User { get; set; } = null!;

  /// <summary>
  ///   The feedback comment object associated with this.
  /// </summary>
  public FeedbackComment FeedbackComment { get; set; } = null!;

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<FeedbackCommentReadReceipt>(entity => {
      entity.HasOne(e => e.User)
        .WithMany(e => e.FeedbackCommentReadReceipts)
        .HasForeignKey(e => e.UserId)
        .HasPrincipalKey(e => e.Id);

      entity.HasOne(e => e.FeedbackComment)
        .WithMany(e => e.FeedbackCommentReadReceipts)
        .HasForeignKey(e => e.FeedbackCommentId)
        .HasPrincipalKey(e => e.Id);
    });
  }
}