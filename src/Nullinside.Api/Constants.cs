namespace Nullinside.Api;

/// <summary>
/// Constants used throughout the application.
/// </summary>
public static class Constants {
  /// <summary>
  /// The amount of time a token is valid for.
  /// </summary>
  public static readonly TimeSpan OAUTH_TOKEN_TIME_LIMIT = TimeSpan.FromHours(1);
}