namespace Nullinside.Api.Shared.Json;

/// <summary>
///   The feedback submitted by users.
/// </summary>
public class ContactUsFeedback {
  /// <summary>
  ///   The product the feedback is for.
  /// </summary>
  public string? Product { get; set; } = null!;

  /// <summary>
  ///   The message.
  /// </summary>
  public string? Message { get; set; } = null!;
}