using System.Collections.ObjectModel;

using TwitchLib.Client.Models;

namespace Nullinside.Api.Common.Twitch.Support;

/// <summary>
///   Represents the message and metadata of a twitch chat message.
/// </summary>
public class TwitchChatMessage {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessage" />.
  /// </summary>
  /// <param name="timestamp">The unix timestamp of the message.</param>
  /// <param name="channel">The twitch channel the message was sent in.</param>
  /// <param name="username">The user that sent the message.</param>
  /// <param name="message">The message sent in twitch chat.</param>
  /// <param name="firstTimeMessage">
  ///   True if the message is the first message sent by the user in this channel ever, false
  ///   otherwise.
  /// </param>
  /// <param name="userId">The unique identifier of the user from twitch.</param>
  /// <param name="displayName">The display name of the user per the user's twitch settings.</param>
  /// <param name="channelId">The unique identifier of the channel from twitch.</param>
  /// <param name="emotes">The emotes available in the channel.</param>
  public TwitchChatMessage(DateTimeOffset timestamp, string channel, string username, string message, bool firstTimeMessage, string userId, string displayName, string channelId, IEnumerable<TwitchEmote> emotes) {
    Emotes = emotes.Select(e => new Emote(e.Id, e.Name, e.StartIndex, e.EndIndex)).ToList().AsReadOnly();
    Timestamp = timestamp.UtcDateTime;
    Channel = channel;
    Message = message;
    IsFirstTimeMessage = firstTimeMessage;
    UserId = userId;
    DisplayName = displayName;
    ChannelId = channelId;
    Username = username;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessage" /> from a <see cref="ChatMessage" />.
  /// </summary>
  /// <param name="source">The original message to pull fields from.</param>
  public TwitchChatMessage(ChatMessage source) {
    Emotes = source.EmoteSet.Emotes.AsReadOnly();
    ChannelId = source.RoomId;
    DisplayName = source.DisplayName;
    UserId = source.UserId;
    Username = source.Username;
    Timestamp = source.TmiSent.UtcDateTime;
    Channel = source.Channel;
    Message = source.Message;
    IsFirstTimeMessage = source.IsFirstMessage;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessage" /> from a <see cref="ChatMessage" />.
  /// </summary>
  public TwitchChatMessage() {
    Emotes = new List<Emote>().AsReadOnly();
    ChannelId = string.Empty;
    DisplayName = string.Empty;
    UserId = string.Empty;
    Username = string.Empty;
    Timestamp = DateTime.MinValue;
    Channel = string.Empty;
    Message = string.Empty;
    IsFirstTimeMessage = false;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchChatMessage" /> from a <see cref="ChatMessage" />.
  /// </summary>
  /// <param name="ircMessage">The raw IRC message.</param>
  public TwitchChatMessage(string ircMessage) {
    Emotes = Enumerable.Empty<Emote>().ToList().AsReadOnly();
    Timestamp = DateTime.MinValue;
    Channel = string.Empty;
    Message = string.Empty;
    UserId = string.Empty;
    DisplayName = string.Empty;
    ChannelId = string.Empty;
    Username = string.Empty;

    if (!ircMessage.Contains(" PRIVMSG ")) {
      return;
    }

    int tagEnd = ircMessage.IndexOf(' ');
    if (tagEnd < 0 || !ircMessage.StartsWith('@')) {
      return;
    }

    string tagSection = ircMessage[1..tagEnd];

    Dictionary<string, string> tags = ParseTags(tagSection);

    string remainder = ircMessage[(tagEnd + 1)..];

    // :foo!foo@foo.tmi.twitch.tv PRIVMSG #channel :hello
    int exclamation = remainder.IndexOf('!');
    if (exclamation < 1) {
      return;
    }

    string username = remainder[1..exclamation];

    int privmsgIndex = remainder.IndexOf(" PRIVMSG ", StringComparison.InvariantCultureIgnoreCase);
    if (privmsgIndex < 0) {
      return;
    }

    string afterPrivMsg = remainder[(privmsgIndex + " PRIVMSG ".Length)..];

    int channelEnd = afterPrivMsg.IndexOf(' ');
    if (channelEnd < 0) {
      return;
    }

    string channel = afterPrivMsg[..channelEnd].TrimStart('#');

    int messageIndex = afterPrivMsg.IndexOf(" :", StringComparison.InvariantCultureIgnoreCase);
    if (messageIndex < 0) {
      return;
    }

    string chatMessage = afterPrivMsg[(messageIndex + 2)..];

    tags.TryGetValue("display-name", out string? displayName);
    tags.TryGetValue("user-id", out string? userId);
    tags.TryGetValue("room-id", out string? roomId);
    tags.TryGetValue("first-msg", out string? firstMsg);
    tags.TryGetValue("mod", out string? mod);
    tags.TryGetValue("emotes", out string? emotes);
    tags.TryGetValue("tmi-sent-ts", out string? timestamp);

    Emotes = ParseEmotes(chatMessage, emotes).ToList().AsReadOnly();
    Timestamp = ToDateTimeOffset(timestamp).UtcDateTime;
    Channel = channel;
    Message = chatMessage;
    IsFirstTimeMessage = firstMsg == "1";
    UserId = userId ?? "";
    DisplayName = displayName ?? username;
    ChannelId = roomId ?? "";
    Username = username;
  }

  /// <summary>
  ///   The utc timestamp of the message.
  /// </summary>
  public DateTime Timestamp { get; private set; }

  /// <summary>
  ///   The twitch channel the message was sent in.
  /// </summary>
  public string Channel { get; private set; }

  /// <summary>
  ///   The message sent in twitch chat.
  /// </summary>
  public string Message { get; private set; }

  /// <summary>
  ///   The user that send the message.
  /// </summary>
  public string Username { get; private set; }

  /// <summary>
  ///   The unique identifier of the user from twitch.
  /// </summary>
  public string UserId { get; private set; }

  /// <summary>
  ///   The username formatted for display to the user per the user's twitch settings.
  /// </summary>
  public string DisplayName { get; private set; }

  /// <summary>
  ///   The unique identifier of the channel from twitch.
  /// </summary>
  public string ChannelId { get; private set; }

  /// <summary>
  ///   The emotes available in the channel.
  /// </summary>
  public ReadOnlyCollection<Emote> Emotes { get; private set; }

  /// <summary>
  ///   True if the message is the first message sent by the user in this channel ever, false otherwise.
  /// </summary>
  /// <remarks>
  ///   "first message ever" refers to never having spoken in the channel, not the first time they sent a message
  ///   in the current stream
  /// </remarks>
  public bool IsFirstTimeMessage { get; private set; }

  /// <summary>
  ///   Parses the tags from the IRC message.
  /// </summary>
  /// <param name="tagSegment">The tag segment of the message.</param>
  /// <returns>A dictionary of tags and their values.</returns>
  private static Dictionary<string, string> ParseTags(string tagSegment) {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (string pair in tagSegment.Split(';')) {
      int equals = pair.IndexOf('=');

      if (equals < 0) {
        result[pair] = "";
        continue;
      }

      string key = pair[..equals];
      string value = pair[(equals + 1)..];

      result[key] = value;
    }

    return result;
  }

  /// <summary>
  ///   Parses the emotes from the message.
  /// </summary>
  /// <param name="message">The message.</param>
  /// <param name="emotesSeg">The emotes segment of the message.</param>
  /// <returns>A list of emotes found in the message.</returns>
  private List<Emote> ParseEmotes(string message, string? emotesSeg) {
    var result = new List<Emote>();
    if (string.IsNullOrWhiteSpace(emotesSeg)) {
      return result;
    }

    foreach (string emotePart in emotesSeg.Split('/')) {
      string[] pieces = emotePart.Split(':', 2);
      if (pieces.Length != 2) {
        continue;
      }

      string id = pieces[0];

      List<(int Start, int End)> positions = pieces[1]
        .Split(',')
        .Select(x => {
          string[] range = x.Split('-');
          return (
            Start: int.Parse(range[0]),
            End: int.Parse(range[1]));
        })
        .ToList();

      string emote = message.Substring(positions[0].Start, positions[0].End - positions[0].Start);
      result.AddRange(positions.Select(p => new Emote(id, emote, p.Start, p.End)));
    }

    return result;
  }

  /// <summary>
  ///   Converts unix milliseconds to DateTimeOffset.
  /// </summary>
  /// <param name="unixMilliseconds">The string representation of unix milliseconds.</param>
  /// <returns>The datetime offset.</returns>
  private DateTimeOffset ToDateTimeOffset(string? unixMilliseconds) {
    if (!long.TryParse(unixMilliseconds, out long milliseconds)) {
      return DateTimeOffset.MinValue;
    }

    return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
  }
}