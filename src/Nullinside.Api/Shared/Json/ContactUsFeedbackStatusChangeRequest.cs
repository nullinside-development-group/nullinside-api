namespace Nullinside.Api.Shared.Json;

/// <summary>
///   A request to change the status of feedback.
/// </summary>
public class ContactUsFeedbackStatusChangeRequest {
  /// <summary>
  ///   The status to change the feedback to.
  /// </summary>
  public string? Status { get; set; }
}