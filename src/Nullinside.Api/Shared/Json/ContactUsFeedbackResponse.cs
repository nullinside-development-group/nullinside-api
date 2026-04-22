using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Shared.Json;

/// <summary>
///   The response object for returning contact us feedback.
/// </summary>
public class ContactUsFeedbackResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="ContactUsFeedbackResponse" /> class.
  /// </summary>
  /// <param name="feedback">The feedback object to copy data from.</param>
  /// <param name="isAdmin">True if the user requesting the data is an admin, false otherwise.</param>
  /// <param name="callerUserId">
  ///   The user id of the person making the request, used to determine if they've read the feedback
  ///   and comments.
  /// </param>
  public ContactUsFeedbackResponse(Feedback feedback, bool isAdmin, int callerUserId) {
    Id = feedback.Id;
    UserId = feedback.UserId;
    Product = feedback.Product;
    Message = feedback.Message;
    Status = feedback.Status.ToString();
    Timestamp = feedback.Timestamp;
    Email = isAdmin ? feedback.User.Email : null;
    IsRead = callerUserId == UserId || null != feedback.FeedbackReadReceipts.FirstOrDefault(r => r.UserId == callerUserId);
    Comments = feedback.Comments.Select(c => new ContactUsFeedbackCommentResponse(
      c.Id,
      c.UserId,
      Email = isAdmin ? c.User.Email : null,
      feedback.UserId == callerUserId ? "You" : "Site Admin",
      c.Message,
      c.Timestamp,
      c.UserId == callerUserId || null != c.FeedbackCommentReadReceipts.FirstOrDefault(r => r.UserId == callerUserId)
    ));
  }

  /// <summary>
  ///   The unique identifier for the feedback.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The user id who submitted the feedback.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   True if the feedback has been read, false otherwise.
  /// </summary>
  public bool IsRead { get; set; }

  /// <summary>
  ///   The email address of the user, ONLY available to users with the admin role.
  /// </summary>
  public string? Email { get; set; }

  /// <summary>
  ///   The product the feedback is for.
  /// </summary>
  public string Product { get; set; }

  /// <summary>
  ///   The message.
  /// </summary>
  public string Message { get; set; }

  /// <summary>
  ///   The current status of the feedback.
  /// </summary>
  public string Status { get; set; }

  /// <summary>
  ///   The timestamp when the feedback was submitted.
  /// </summary>
  public DateTime Timestamp { get; set; }

  /// <summary>
  ///   The comments associated with this feedback.
  /// </summary>
  public IEnumerable<ContactUsFeedbackCommentResponse> Comments { get; set; }

  /// <summary>
  ///   The comment associated with the feedback.
  /// </summary>
  public class ContactUsFeedbackCommentResponse {
    /// <summary>
    ///   Initializes a new instance of the <see cref="ContactUsFeedbackCommentResponse" /> class.
    /// </summary>
    /// <param name="id">The unique identifier for the comment.</param>
    /// <param name="userId">The user id who created the comment.</param>
    /// <param name="email">The email address of the user who created the comment.</param>
    /// <param name="user">The display name of the user who created the comment.</param>
    /// <param name="message">The comment content.</param>
    /// <param name="timestamp">The timestamp when the comment was created.</param>
    /// <param name="isRead">True if the comment has been read, false otherwise.</param>
    public ContactUsFeedbackCommentResponse(int id, int userId, string? email, string user, string message, DateTime timestamp, bool isRead) {
      Id = id;
      UserId = userId;
      User = user;
      Email = email;
      Message = message;
      Timestamp = timestamp;
      IsRead = isRead;
    }

    /// <summary>
    ///   The unique identifier for the comment.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///   The user id who created the comment.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    ///   True if the feedback has been read, false otherwise.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    ///   The email address of the user that made the comment.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///   The display name of the user who created the comment.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    ///   The comment content.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///   The timestamp when the comment was created.
    /// </summary>
    public DateTime Timestamp { get; set; }
  }
}