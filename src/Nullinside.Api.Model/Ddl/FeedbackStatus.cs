namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   The current status of feedback submitted to the site.
/// </summary>
public enum FeedbackStatus {
  /// <summary>
  ///   The feedback is open by default.
  /// </summary>
  Open,

  /// <summary>
  ///   The feedback has been completed.
  /// </summary>
  Completed,

  /// <summary>
  ///   The feedback was rejected.
  /// </summary>
  Rejected
}