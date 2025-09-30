namespace Nullinside.Api.Common.Auth;

/// <summary>
///   Represents an OAuth token in the Twitch workflow.
/// </summary>
public class OAuthToken {
  /// <summary>
  ///   The Twitch access token.
  /// </summary>
  public string? AccessToken { get; set; }

  /// <summary>
  ///   The refresh token.
  /// </summary>
  public string? RefreshToken { get; set; }

  /// <summary>
  ///   The UTC <see cref="DateTime" /> when the <see cref="AccessToken" /> expires.
  /// </summary>
  public DateTime? ExpiresUtc { get; set; }
}