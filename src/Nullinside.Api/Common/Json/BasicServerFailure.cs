namespace Nullinside.Api.Common.Json;

/// <summary>
///   Represents a basic error where you just want to give the caller an error message and nothing more.
/// </summary>
public class BasicServerFailure {
  /// <summary>
  ///   Initializes a new instance of the <see cref="BasicServerFailure" /> class.
  /// </summary>
  /// <param name="error">The error message.</param>
  public BasicServerFailure(string error) {
    Error = error;
  }

  /// <summary>
  ///   Gets or sets the error message.
  /// </summary>
  public string Error { get; set; }
}