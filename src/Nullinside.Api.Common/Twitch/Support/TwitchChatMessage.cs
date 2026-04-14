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
}