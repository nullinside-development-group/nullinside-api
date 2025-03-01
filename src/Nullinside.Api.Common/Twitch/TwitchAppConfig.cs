namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   The configuration for a twitch app that provides OAuth tokens.
/// </summary>
public class TwitchAppConfig {
  /// <summary>
  ///   The client id.
  /// </summary>
  public string? ClientId { get; set; }

  /// <summary>
  ///   The client secret.
  /// </summary>
  public string? ClientSecret { get; set; }

  /// <summary>
  ///   A registered URL that the Twitch API is allowed to redirect to on our website.
  /// </summary>
  public string? ClientRedirect { get; set; }
}