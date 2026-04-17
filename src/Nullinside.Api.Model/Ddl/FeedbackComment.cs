using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   Represents docker deployments both in container form as well as docker compose project form.
/// </summary>
public class FeedbackComment : ITableModel {
  /// <summary>
  ///   Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The nullinside database feedback id of the feedback this comment was submitted against.
  /// </summary>
  public int FeedbackId { get; set; }

  /// <summary>
  ///   The nullinside database user id of the user who submitted the comment.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The feedback.
  /// </summary>
  public string Message { get; set; } = null!;
  
  /// <summary>
  ///   The timestamp of when the feedback was submitted.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The user object associated with this.
  /// </summary>
  public User User { get; set; } = null!;

  /// <summary>
  ///   The feedback this comment is attached to.
  /// </summary>
  public Feedback Feedback { get; set; } = null!;

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<FeedbackComment>(entity => {
      entity.HasOne(e => e.User)
        .WithMany(e => e.FeedbackComments)
        .HasForeignKey(e => e.UserId)
        .HasPrincipalKey(e => e.Id);
      
      entity.HasOne(e => e.Feedback)
        .WithMany(e => e.Comments)
        .HasForeignKey(e => e.FeedbackId)
        .HasPrincipalKey(e => e.Id);

      entity.Property(e => e.Message)
        .HasMaxLength(10000)
        .IsRequired();
    });
  }
}