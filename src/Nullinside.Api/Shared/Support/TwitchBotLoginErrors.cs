namespace Nullinside.Api.Shared.Support;

/// <summary>
/// Enumerates the types of errors when authenticating with twitch.
/// </summary>
public enum TwitchBotLoginErrors {
  /// <summary>
  /// An error with the token that twitch provided us.
  /// </summary>
  TwitchErrorWithToken,
  /// <summary>
  /// The twitch account doesn't have an email address associated with it.
  /// </summary>
  TwitchAccountHasNoEmail,
  /// <summary>
  /// An internal error in the web server having nothing to do with the outside world.
  /// </summary>
  InternalError
}