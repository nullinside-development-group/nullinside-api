namespace Nullinside.Api.Common.Twitch.Support;

/// <summary>
///   Twitch user information.
/// </summary>
public class TwitchUserInfo {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchUserInfo" /> class.
  /// </summary>
  /// <param name="username">The username used to log in.</param>
  /// <param name="displayName">The formatted username for display.</param>
  /// <param name="id">The unique identifier for the user.</param>
  public TwitchUserInfo(string username, string displayName, string id) {
    Username = username;
    DisplayName = displayName;
    Id = id;
  }

  /// <summary>
  ///   The username used to log in.
  /// </summary>
  public string Username { get; set; }

  /// <summary>
  ///   The formatted username for display.
  /// </summary>
  public string DisplayName { get; set; }

  /// <summary>
  ///   The unique identifier for the user.
  /// </summary>
  public string Id { get; set; }
}