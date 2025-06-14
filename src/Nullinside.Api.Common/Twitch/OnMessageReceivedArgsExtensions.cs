using TwitchLib.Client.Events;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   Extensions for the <see cref="OnMessageReceivedArgs" /> class to make working with twitch chat messages easier.
/// </summary>
public static class OnMessageReceivedArgsExtensions {
  /// <summary>
  ///   Gets the timestamp of when the message was sent, in UTC.
  /// </summary>
  /// <param name="e">The event arguments.</param>
  /// <returns>The <see cref="DateTime" /> if successful, null otherwise.</returns>
  public static DateTime? GetTimestamp(this OnMessageReceivedArgs e) {
    if (double.TryParse(e.ChatMessage.TmiSentTs, out double timestampD)) {
      return DateTimeExtensions.FromUnixTimestamp(timestampD);
    }

    return null;
  }
}