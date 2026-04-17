using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Shared.Json;

/// <summary>
/// The response object for returning contact us feedback.
/// </summary>
public class ContactUsFeedbackResponse {
  /// <summary>
  /// The comment associated with the feedback.
  /// </summary>
  public class ContactUsFeedbackCommentResponse {
    /// <summary>
    /// The unique identifier for the comment.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The user id who created the comment.
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// The display name of the user who created the comment.
    /// </summary>
    public string User { get; set; } = null!;
    
    /// <summary>
    /// The comment content.
    /// </summary>
    public string Message { get; set; } = null!;
    
    /// <summary>
    /// The timestamp when the comment was created.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactUsFeedbackCommentResponse"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the comment.</param>
    /// <param name="userId">The user id who created the comment.</param>
    /// <param name="user">The display name of the user who created the comment.</param>
    /// <param name="message">The comment content.</param>
    /// <param name="timestamp">The timestamp when the comment was created.</param>
    public ContactUsFeedbackCommentResponse(int id, int userId, string user, string message, DateTime timestamp) {
      Id = id;
      UserId = userId;
      User = user;
      Message = message;
      Timestamp = timestamp;
    }
  }
  
  /// <summary>
  /// The unique identifier for the feedback.
  /// </summary>
  public int Id { get; set; }
  
  /// <summary>
  /// The user id who submitted the feedback.
  /// </summary>
  public int UserId { get; set; }
  
  /// <summary>
  /// The product the feedback is for.
  /// </summary>
  public string Product { get; set; }
  
  /// <summary>
  /// The message.
  /// </summary>
  public string Message { get; set; }
  
  /// <summary>
  /// The current status of the feedback.
  /// </summary>
  public string Status { get; set; }
  
  /// <summary>
  /// The timestamp when the feedback was submitted.
  /// </summary>
  public DateTime Timestamp { get; set; }
  
  /// <summary>
  /// The comments associated with this feedback.
  /// </summary>
  public IEnumerable<ContactUsFeedbackCommentResponse> Comments { get; set; }

  /// <summary>
  /// Initializes a new instance of the <see cref="ContactUsFeedbackResponse"/> class.
  /// </summary>
  /// <param name="feedback">The feedback object to copy data from.</param>
  public ContactUsFeedbackResponse(Feedback feedback) {
    Id = feedback.Id;
    UserId = feedback.UserId;
    Product = feedback.Product;
    Message = feedback.Message;
    Status = feedback.Status.ToString();
    Timestamp = feedback.Timestamp;
    Comments = feedback.Comments.Select(c => new ContactUsFeedbackCommentResponse(
      c.Id,
      c.UserId,
      feedback.UserId == c.UserId ? "You" : "Site Admin",
      c.Message,
      c.Timestamp
    ));
  }
}