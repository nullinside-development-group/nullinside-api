using System.Diagnostics;

using log4net;

using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   A proxy for making twitch requests.
/// </summary>
public class TwitchApiProxy {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog Log = LogManager.GetLogger(typeof(TwitchApiProxy));

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchApiProxy" /> class.
  /// </summary>
  public TwitchApiProxy() {
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchApiProxy" /> class.
  /// </summary>
  /// <param name="token">The access token.</param>
  /// <param name="refreshToken">The refresh token.</param>
  /// <param name="tokenExpires">When the token expires (utc).</param>
  public TwitchApiProxy(string token, string refreshToken, DateTime tokenExpires) {
    AccessToken = token;
    RefreshToken = refreshToken;
    ExpiresUtc = tokenExpires;
  }

  /// <summary>
  ///   The, public, twitch client id.
  /// </summary>
  public static string ClientId { get; } = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_ID")!;

  /// <summary>
  ///   The, private, twitch client secret.
  /// </summary>
  public static string ClientSecret { get; } = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_SECRET")!;

  /// <summary>
  ///   The redirect url.
  /// </summary>
  public static string ClientRedirect { get; } = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_REDIRECT")!;

  /// <summary>
  ///   The Twitch access token.
  /// </summary>
  public string? AccessToken { get; set; }

  /// <summary>
  ///   The refresh token.
  /// </summary>
  public string? RefreshToken { get; set; }

  /// <summary>
  ///   The UTC <see cref="DateTime" /> when the <see cref="AccessToken" /> expires.
  /// </summary>
  public DateTime? ExpiresUtc { get; set; }

  /// <summary>
  ///   The number of times to retry queries before giving up.
  /// </summary>
  public int Retries { get; set; } = 3;

  /// <summary>
  ///   Gets a new instance of the <see cref="TwitchAPI" />.
  /// </summary>
  /// <returns>A new instance of the <see cref="TwitchAPI" />.</returns>
  private TwitchAPI GetApi() {
    var api = new TwitchAPI {
      Settings = {
        ClientId = ClientId,
        AccessToken = AccessToken
      }
    };
    return api;
  }

  /// <summary>
  ///   Refreshes the access token.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <remarks>The object will have it's properties updated with the new settings for the token.</remarks>
  /// <returns>True if successful, false otherwise.</returns>
  public async Task<bool> RefreshTokenAsync(CancellationToken token = new()) {
    try {
      TwitchAPI api = GetApi();
      RefreshResponse? response = await api.Auth.RefreshAuthTokenAsync(RefreshToken, ClientSecret, ClientId);
      if (null == response) {
        return false;
      }

      AccessToken = response.AccessToken;
      RefreshToken = response.RefreshToken;
      ExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
      return true;
    }
    catch (Exception) {
      return false;
    }
  }

  /// <summary>
  ///   Gets a new access token.
  /// </summary>
  /// <param name="code">The code to send to twitch to generate a new access token.</param>
  /// <param name="token">The cancellation token.</param>
  /// <remarks>The object will have it's properties updated with the new settings for the token.</remarks>
  /// <returns>True if successful, false otherwise.</returns>
  public async Task<bool> GetAccessToken(string code, CancellationToken token = new()) {
    TwitchAPI api = GetApi();
    AuthCodeResponse? response = await api.Auth.GetAccessTokenFromCodeAsync(code, ClientSecret, ClientRedirect);
    if (null == response) {
      return false;
    }

    AccessToken = response.AccessToken;
    RefreshToken = response.RefreshToken;
    ExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
    return true;
  }

  /// <summary>
  ///   Gets the email address of the owner of the <see cref="AccessToken" />.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The email address if successful, null otherwise.</returns>
  public async Task<string?> GetUserEmail(CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();
      GetUsersResponse? response = await api.Helix.Users.GetUsersAsync();
      if (null == response) {
        return null;
      }

      return response.Users.FirstOrDefault()?.Email;
    }, Retries, token);
  }

  /// <summary>
  ///   Gets the twitch username of the owner of the <see cref="AccessToken" />.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The twitch username if successful, null otherwise.</returns>
  public async Task<(string? id, string? username)> GetTwitchUser(CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();
      GetUsersResponse? response = await api.Helix.Users.GetUsersAsync();
      if (null == response) {
        return (null, null);
      }

      User? user = response.Users.FirstOrDefault();
      return (user?.Id, user?.Login);
    }, Retries, token);
  }

  /// <summary>
  ///   Determines if the API has valid credentials.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if successful, null otherwise.</returns>
  public async Task<bool> IsValid(CancellationToken token = new()) {
    return !string.IsNullOrWhiteSpace((await GetTwitchUser(token)).id);
  }

  /// <summary>
  ///   Gets the chatters in a channel.
  /// </summary>
  /// <param name="channelId">The id of the channel that we are moderating.</param>
  /// <param name="botId">The id of the bot channel.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The collection of chatters.</returns>
  public async Task<IEnumerable<Chatter>> GetChattersInChannel(string channelId, string botId,
    CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();
      var chatters = new List<Chatter>();
      string? cursor = null;
      int total = 0;
      do {
        GetChattersResponse? response = await api.Helix.Chat.GetChattersAsync(channelId, botId, 1000, cursor);
        if (null == response) {
          break;
        }

        chatters.AddRange(response.Data);
        cursor = response.Pagination.Cursor;
        total = response.Total;
      } while (null != cursor);

      Debug.Assert(chatters.Count == total);
      return chatters;
    }, Retries, token);
  }

  /// <summary>
  ///   Bans a list of users.
  /// </summary>
  /// <param name="channelId">The twitch account id of the channel to ban the users from.</param>
  /// <param name="botId">The twitch account id of the bot user.</param>
  /// <param name="users">The list of users to ban.</param>
  /// <param name="reason">The reason for the ban.</param>
  /// <param name="token">The stopping token.</param>
  /// <returns>The users with confirmed bans.</returns>
  public async Task<IEnumerable<BannedUser>> BanUsers(string channelId, string botId,
    IEnumerable<(string Id, string Username)> users, string reason, CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();

      var bannedUsers = new List<BannedUser>();
      foreach ((string Id, string Username) user in users) {
        try {
          BanUserResponse? response = await api.Helix.Moderation.BanUserAsync(channelId, botId, new BanUserRequest {
            UserId = user.Id,
            Reason = reason
          });

          if (null == response || null == response.Data) {
            continue;
          }

          bannedUsers.AddRange(response.Data);
        }
        catch (HttpResponseException ex) {
          string exceptionReason = await ex.HttpResponse.Content.ReadAsStringAsync(token);
          Log.Debug($"Failed to ban {user.Username} ({user.Id}) in {channelId}: {exceptionReason}", ex);
        }
        catch (Exception ex) {
          Log.Debug($"Failed to ban {user.Username} ({user.Id}) in {channelId}", ex);
        }

        return bannedUsers;
      }

      return Enumerable.Empty<BannedUser>();
    }, Retries, token);
  }

  /// <summary>
  ///   Gets the list of mods for the channel.
  /// </summary>
  /// <param name="channelId">The twitch id of the channel to get mods for.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The collection of moderators.</returns>
  public async Task<IEnumerable<Moderator>> GetMods(string channelId, CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();

      var results = new List<Moderator>();
      GetModeratorsResponse? response = null;
      do {
        response = await api.Helix.Moderation.GetModeratorsAsync(channelId, first: 100,
          after: response?.Pagination?.Cursor);
        if (null == response || null == response.Data) {
          break;
        }


        Moderator[]? data = response.Data;
        if (null == data) {
          continue;
        }

        results.AddRange(data);
      } while (null != response.Pagination?.Cursor);

      return results;
    }, Retries, token);
  }

  /// <summary>
  ///   Makes the bot account a mod.
  /// </summary>
  /// <param name="channelId">The twitch id of the channel to add the mod to.</param>
  /// <param name="userId">The twitch user id to mod.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>True if successful, false otherwise.</returns>
  public async Task<bool> ModAccount(string channelId, string userId, CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();
      await api.Helix.Moderation.AddChannelModeratorAsync(channelId, userId);
      return true;
    }, Retries, token);
  }
}