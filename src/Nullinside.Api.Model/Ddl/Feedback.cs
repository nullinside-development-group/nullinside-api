using Microsoft.EntityFrameworkCore;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   Represents docker deployments both in container form as well as docker compose project form.
/// </summary>
public class Feedback : ITableModel {
  /// <summary>
  ///   Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The nullinside database user id of the user who submitted the feedback.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The current status of the feedback.
  /// </summary>
  public FeedbackStatus Status { get; set; }

  /// <summary>
  ///   The product the feedback was opened against
  /// </summary>
  public string Product { get; set; } = null!;

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
  ///   The comments associated with this.
  /// </summary>
  public ICollection<FeedbackComment> Comments { get; set; } = null!;

  /// <summary>
  ///   The collection of receipts for when the feedback was read.
  /// </summary>
  public ICollection<FeedbackReadReceipt> FeedbackReadReceipts { get; set; } = null!;

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<Feedback>(entity => {
      entity.HasOne(e => e.User)
        .WithMany(e => e.Feedbacks)
        .HasForeignKey(e => e.UserId)
        .HasPrincipalKey(e => e.Id);

      entity.HasMany(e => e.Comments)
        .WithOne(e => e.Feedback)
        .HasForeignKey(e => e.FeedbackId)
        .HasPrincipalKey(e => e.Id);

      entity.Property(e => e.Product)
        .HasMaxLength(50)
        .IsRequired();

      entity.Property(e => e.Message)
        .HasMaxLength(10000)
        .IsRequired();
    });
  }
}