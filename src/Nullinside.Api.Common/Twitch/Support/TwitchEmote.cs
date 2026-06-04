using TwitchLib.Client.Models;

namespace Nullinside.Api.Common.Twitch.Support;

/// <summary>
///   A twitch chat channel emote.
/// </summary>
public class TwitchEmote {
  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchEmote" /> class.
  /// </summary>
  /// <param name="emoteId">Twitch-assigned emote Id.</param>
  /// <param name="name">
  ///   The name of the emote. For example, if the message was "This is Kappa test.", the name would be
  ///   'Kappa'.
  /// </param>
  /// <param name="emoteStartIndex">
  ///   Character starting index. For example, if the message was "This is Kappa test.", the
  ///   start index would be 8 for 'Kappa'.
  /// </param>
  /// <param name="emoteEndIndex">
  ///   Character ending index. For example, if the message was "This is Kappa test.", the start
  ///   index would be 12 for 'Kappa'.
  /// </param>
  public TwitchEmote(string emoteId, string name, int emoteStartIndex, int emoteEndIndex) {
    Id = emoteId;
    Name = name;
    StartIndex = emoteStartIndex;
    EndIndex = emoteEndIndex;
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchEmote" /> class.
  /// </summary>
  /// <param name="emote"></param>
  public TwitchEmote(Emote emote) {
    Id = emote.Id;
    Name = emote.Name;
    StartIndex = emote.StartIndex;
    EndIndex = emote.EndIndex;
  }

  /// <summary>
  ///   Twitch-assigned emote Id.
  /// </summary>
  public string Id { get; }

  /// <summary>
  ///   The name of the emote. For example, if the message was "This is Kappa test.", the name would be 'Kappa'.
  /// </summary>
  public string Name { get; }

  /// <summary>
  ///   Character starting index. For example, if the message was "This is Kappa test.", the start index would be 8 for
  ///   'Kappa'.
  /// </summary>
  public int StartIndex { get; }

  /// <summary>
  ///   Character ending index. For example, if the message was "This is Kappa test.", the start index would be 12 for
  ///   'Kappa'.
  /// </summary>
  public int EndIndex { get; }
}