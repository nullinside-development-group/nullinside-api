namespace Nullinside.Api.Shared.Json;

using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

/// <summary>
///   A response with information about live Twitch streams.
/// </summary>
public class TwitchLiveUsersResponse {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchLiveUsersResponse" /> class.
  /// </summary>
  /// <param name="stream">The stream to pull information from.</param>
  public TwitchLiveUsersResponse(Stream stream) {
    TwitchId = stream.UserId;
    ViewerCount = stream.ViewerCount;
    GoneLiveTime = stream.StartedAt;
    Username = stream.UserName;
    StreamTitle = stream.Title;
    GameName = stream.GameName;
    ThumbnailUrl = stream.ThumbnailUrl;
  }

  /// <summary>
  ///   The unique identifier for the twitch user from twitch.
  /// </summary>
  public string TwitchId { get; set; }

  /// <summary>
  ///   The total view count for the stream.
  /// </summary>
  public int ViewerCount { get; set; }

  /// <summary>
  ///   The time the stream went live.
  /// </summary>
  public DateTime GoneLiveTime { get; set; }

  /// <summary>
  ///   The username of the twitch user.
  /// </summary>
  public string Username { get; set; }

  /// <summary>
  ///   The title of the stream.
  /// </summary>
  public string StreamTitle { get; set; }

  /// <summary>
  ///   The name of the game being played on the stream.
  /// </summary>
  public string GameName { get; set; }

  /// <summary>
  ///   The url of the twitch generated thumbnail for the stream.
  /// </summary>
  public string ThumbnailUrl { get; set; }
}