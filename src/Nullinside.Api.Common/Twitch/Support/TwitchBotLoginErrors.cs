namespace Nullinside.Api.Common.Twitch.Support;

/// <summary>
///   Enumerates the types of errors when authenticating with twitch.
/// </summary>
public enum TwitchBotLoginErrors {
  /// <summary>
  ///   An error with the token that twitch provided us.
  /// </summary>
  TWITCH_ERROR_WITH_TOKEN,

  /// <summary>
  ///   The twitch account doesn't have an email address associated with it.
  /// </summary>
  TWITCH_ACCOUNT_HAS_NO_EMAIL,

  /// <summary>
  ///   An internal error in the web server having nothing to do with the outside world.
  /// </summary>
  INTERNAL_ERROR
}