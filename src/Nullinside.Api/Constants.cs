namespace Nullinside.Api;

/// <summary>
///   Constants used throughout the application.
/// </summary>
public static class Constants {
  /// <summary>
  ///   The account id of the admin user.
  /// </summary>
  public const int ADMIN_USER_ID = 1;

  /// <summary>
  ///   The maximum number of unread contact us messages a user can have.
  /// </summary>
  public const int MAXIMUM_UNREAD_CONTACT_US = 50;

  /// <summary>
  ///   The maximum number of unread contact us messages before we stop sending emails.
  /// </summary>
  public const int MAXIMUM_UNREAD_CONTACT_US_TO_STOP_EMAILING = 5;

  /// <summary>
  ///   The email address associated with the twitch account for the bot.
  /// </summary>
  public const string BOT_EMAIL = "dev.nullinside@gmail.com";

  /// <summary>
  ///   The twitch username for the bot account.
  /// </summary>
  public const string BOT_USERNAME = "nullinside";

  /// <summary>
  ///   The twitch id for the bot account.
  /// </summary>
  public const string BOT_ID = "640082552";

  /// <summary>
  ///   The amount of time a token is valid for.
  /// </summary>
  public static readonly TimeSpan OAUTH_TOKEN_TIME_LIMIT = TimeSpan.FromDays(7);
}