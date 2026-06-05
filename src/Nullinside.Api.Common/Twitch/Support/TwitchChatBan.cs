using System.Text.RegularExpressions;

using TwitchLib.Client.Models;

namespace Nullinside.Api.Common.Twitch.Support;

/// <summary>
///   A twitch chat ban.
/// </summary>
public class TwitchChatBan {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatBan" /> class.
  /// </summary>
  /// <param name="channel">Channel that had ban event.</param>
  /// <param name="username">User that was banned.</param>
  /// <param name="roomId">Channel that had ban event. Id.</param>
  /// <param name="targetUserId">User that was banned. Id.</param>
  /// <param name="banReason">The reason for the ban, if provided.</param>
  public TwitchChatBan(string channel, string username, string roomId, string targetUserId, string banReason) {
    Channel = channel;
    Username = username;
    RoomId = roomId;
    TargetUserId = targetUserId;
    BanReason = banReason;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatBan" /> class.
  /// </summary>
  /// <param name="source">The original ban to pull metadata from.</param>
  public TwitchChatBan(UserBan source) {
    BanReason = string.Empty;
    Channel = source.Channel;
    Username = source.Username;
    RoomId = source.RoomId;
    TargetUserId = source.TargetUserId;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatBan" /> class.
  /// </summary>
  /// <param name="irc">The original ban to pull metadata from.</param>
  public TwitchChatBan(string irc) {
    BanReason = string.Empty;
    Channel = string.Empty;
    Username = string.Empty;
    RoomId = string.Empty;
    TargetUserId = string.Empty;

    if (string.IsNullOrWhiteSpace(irc)) {
      return;
    }

    // Parse IRC tags
    if (!irc.StartsWith("@")) {
      return;
    }

    int tagsEnd = irc.IndexOf(' ');
    if (tagsEnd <= 0) {
      return;
    }

    string[] tags = irc[..tagsEnd].Split(';');

    foreach (string tag in tags) {
      string[] parts = tag.Split('=', 2);
      if (parts.Length != 2) {
        continue;
      }

      switch (parts[0].TrimStart('@')) {
        case "room-id":
          RoomId = parts[1];
          break;

        case "target-user-id":
          TargetUserId = parts[1];
          break;

        case "ban-reason":
          BanReason = parts[1]
            .Replace(@"\s", " ")
            .Replace(@"\:", ";");
          break;
      }
    }

    // Parse channel from CLEARCHAT command
    Match channelMatch = Regex.Match(irc, @"CLEARCHAT\s+#(?<channel>[^\s]+)", RegexOptions.IgnoreCase);
    if (channelMatch.Success) {
      Channel = channelMatch.Groups["channel"].Value;
    }

    // Parse username (trailing parameter)
    Match userMatch = Regex.Match(irc, @"CLEARCHAT\s+#[^\s]+\s+:(?<user>.+)$", RegexOptions.IgnoreCase);
    if (userMatch.Success) {
      Username = userMatch.Groups["user"].Value;
    }
  }

  /// <summary>
  ///   Channel that had ban event.
  /// </summary>
  public string Channel { get; private set; }

  /// <summary>
  ///   User that was banned.
  /// </summary>
  public string Username { get; private set; }

  /// <summary>
  ///   Channel that had ban event. Id.
  /// </summary>
  public string RoomId { get; private set; }

  /// <summary>
  ///   User that was banned. Id.
  /// </summary>
  public string TargetUserId { get; private set; }

  /// <summary>
  ///   The reason provided with the ban, if included.
  /// </summary>
  public string BanReason { get; private set; }
}