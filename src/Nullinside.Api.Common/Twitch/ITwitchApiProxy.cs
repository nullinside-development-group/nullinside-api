using Nullinside.Api.Common.Twitch.Json;

using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   The proxy for handling communication with Twitch.
/// </summary>
public interface ITwitchApiProxy {
  /// <summary>
  ///   The Twitch access token. These are the credentials used for all requests.
  /// </summary>
  TwitchAccessToken? OAuth { get; set; }

  /// <summary>
  ///   The Twitch app configuration. These are used for all requests.
  /// </summary>
  TwitchAppConfig? TwitchAppConfig { get; set; }

  /// <summary>
  ///   Creates a new access token from a code using Twitch's OAuth workflow.
  /// </summary>
  /// <param name="code">The code from twitch to send back to twitch to generate a new access token.</param>
  /// <param name="token">The cancellation token.</param>
  /// <remarks>The object will have its <see cref="OAuth" /> updated with the new settings for the token.</remarks>
  /// <returns>The OAuth details if successful, null otherwise.</returns>
  Task<TwitchAccessToken?> CreateAccessToken(string code, CancellationToken token = new());

  /// <summary>
  ///   Refreshes the access token.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <remarks>The object will have its <see cref="OAuth" /> updated with the new settings for the token.</remarks>
  /// <returns>The OAuth details if successful, null otherwise.</returns>
  Task<TwitchAccessToken?> RefreshAccessToken(CancellationToken token = new());

  /// <summary>
  ///   Determines if the <see cref="OAuth" /> is valid.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if valid, false otherwise.</returns>
  Task<bool> GetAccessTokenIsValid(CancellationToken token = new());

  /// <summary>
  ///   Gets the twitch id and username of the owner of the <see cref="OAuth" />.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The twitch username if successful, null otherwise.</returns>
  Task<(string? id, string? username)> GetUser(CancellationToken token = new());

  /// <summary>
  ///   Gets the email address of the owner of the <see cref="OAuth" />.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The email address if successful, null otherwise.</returns>
  Task<string?> GetUserEmail(CancellationToken token = new());

  /// <summary>
  ///   Gets the list of channels the user moderates for.
  /// </summary>
  /// <param name="userId">The twitch id to scan.</param>
  /// <returns>The list of channels the user moderates for.</returns>
  Task<IEnumerable<TwitchModeratedChannel>> GetUserModChannels(string userId);

  /// <summary>
  ///   Bans a list of users from a channel.
  /// </summary>
  /// <param name="channelId">The twitch id of the channel to ban the users from.</param>
  /// <param name="botId">The twitch id of the bot user, the one banning the users.</param>
  /// <param name="users">The list of users to ban.</param>
  /// <param name="reason">The reason for the ban.</param>
  /// <param name="token">The stopping token.</param>
  /// <returns>The users with confirmed bans.</returns>
  Task<IEnumerable<BannedUser>> BanChannelUsers(string channelId, string botId,
    IEnumerable<(string Id, string Username)> users, string reason, CancellationToken token = new());

  /// <summary>
  ///   Gets the list of mods for the channel.
  /// </summary>
  /// <param name="channelId">The twitch id of the channel to get mods for.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The collection of moderators.</returns>
  Task<IEnumerable<Moderator>> GetChannelMods(string channelId, CancellationToken token = new());

  /// <summary>
  ///   Gets the chatters currently in a channel.
  /// </summary>
  /// <param name="channelId">The twitch id of the channel that we are moderating.</param>
  /// <param name="botId">The twitch id of the bot.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The collection of chatters.</returns>
  Task<IEnumerable<Chatter>> GetChannelUsers(string channelId, string botId, CancellationToken token = new());

  /// <summary>
  ///   Checks if the supplied channels are live.
  /// </summary>
  /// <param name="userIds">The twitch ids of the channels.</param>
  /// <returns>The list of twitch channels that are currently live.</returns>
  Task<IEnumerable<string>> GetChannelsLive(IEnumerable<string> userIds);

  /// <summary>
  ///   Makes a user a moderator in a channel.
  /// </summary>
  /// <param name="channelId">The twitch id of the channel to add the mod to.</param>
  /// <param name="userId">The twitch id to give the moderator role.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if successful, false otherwise.</returns>
  Task<bool> AddChannelMod(string channelId, string userId, CancellationToken token = new());
}