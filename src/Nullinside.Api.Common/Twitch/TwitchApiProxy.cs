using System.Diagnostics;

using log4net;

using Newtonsoft.Json;

using Nullinside.Api.Common.Twitch.Json;

using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   The proxy for handling communication with Twitch.
/// </summary>
public class TwitchApiProxy : ITwitchApiProxy {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog Log = LogManager.GetLogger(typeof(TwitchApiProxy));

  /// <summary>
  ///   The, public, twitch client id.
  /// </summary>
  private static readonly string ClientId = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_ID")!;

  /// <summary>
  ///   The, private, twitch client secret.
  /// </summary>
  private static readonly string ClientSecret = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_SECRET")!;

  /// <summary>
  ///   The redirect url.
  /// </summary>
  private static readonly string ClientRedirect = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_REDIRECT")!;

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
    OAuth = new TwitchAccessToken {
      AccessToken = token,
      RefreshToken = refreshToken,
      ExpiresUtc = tokenExpires
    };
  }

  /// <summary>
  ///   The number of times to retry queries before giving up.
  /// </summary>
  public int Retries { get; set; } = 3;

  /// <inheritdoc />
  public TwitchAccessToken? OAuth { get; set; }

  /// <inheritdoc />
  public async Task<TwitchAccessToken?> CreateAccessToken(string code, CancellationToken token = new()) {
    TwitchAPI api = GetApi();
    AuthCodeResponse? response = await api.Auth.GetAccessTokenFromCodeAsync(code, ClientSecret, ClientRedirect);
    if (null == response) {
      return null;
    }

    OAuth = new TwitchAccessToken {
      AccessToken = response.AccessToken,
      RefreshToken = response.RefreshToken,
      ExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn)
    };
    return OAuth;
  }

  /// <inheritdoc />
  public async Task<TwitchAccessToken?> RefreshAccessToken(CancellationToken token = new()) {
    try {
      TwitchAPI api = GetApi();
      RefreshResponse? response = await api.Auth.RefreshAuthTokenAsync(OAuth?.RefreshToken, ClientSecret, ClientId);
      if (null == response) {
        return null;
      }

      OAuth = new TwitchAccessToken {
        AccessToken = response.AccessToken,
        RefreshToken = response.RefreshToken,
        ExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn)
      };
      return OAuth;
    }
    catch (Exception) {
      return null;
    }
  }

  /// <inheritdoc />
  public async Task<bool> GetAccessTokenIsValid(CancellationToken token = new()) {
    return !string.IsNullOrWhiteSpace((await GetUser(token)).id);
  }

  /// <inheritdoc />
  public async Task<(string? id, string? username)> GetUser(CancellationToken token = new()) {
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

  /// <inheritdoc />
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

  /// <inheritdoc />
  public async Task<IEnumerable<TwitchModeratedChannel>> GetUserModChannels(string userId) {
    using var client = new HttpClient();

    var ret = new List<TwitchModeratedChannel>();
    string? cursor = null;
    do {
      string url = $"https://api.twitch.tv/helix/moderation/channels?user_id={userId}&first=100";
      if (null != cursor) {
        url += $"&after={cursor}";
      }

      var request = new HttpRequestMessage(HttpMethod.Get, url);
      request.Headers.Add("Authorization", $"Bearer {OAuth?.AccessToken}");
      request.Headers.Add("Client-Id", ClientId);

      using HttpResponseMessage response = await client.SendAsync(request);
      response.EnsureSuccessStatusCode();
      string responseBody = await response.Content.ReadAsStringAsync();
      var moderatedChannels = JsonConvert.DeserializeObject<TwitchModeratedChannelsResponse>(responseBody);
      if (null == moderatedChannels) {
        break;
      }

      ret.AddRange(moderatedChannels.data);
      cursor = moderatedChannels.pagination.cursor;
    } while (null != cursor);

    return ret;
  }

  /// <inheritdoc />
  public async Task<IEnumerable<BannedUser>> BanChannelUsers(string channelId, string botId,
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
          Log.Info($"Banned {user.Username} ({user.Id}) in {channelId}: {reason}");
        }
        catch (HttpResponseException ex) {
          string exceptionReason = await ex.HttpResponse.Content.ReadAsStringAsync(token);
          Log.Debug($"Failed to ban {user.Username} ({user.Id}) in {channelId}: {exceptionReason}", ex);
        }
        catch (Exception ex) {
          Log.Debug($"Failed to ban {user.Username} ({user.Id}) in {channelId}", ex);
        }
      }

      return bannedUsers;
    }, Retries, token);
  }

  /// <inheritdoc />
  public async Task<IEnumerable<Chatter>> GetChannelUsers(string channelId, string botId,
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

  /// <inheritdoc />
  public async Task<IEnumerable<string>> GetChannelsLive(IEnumerable<string> userIds) {
    TwitchAPI api = GetApi();

    // We can only query 100 at a time, so throttle the search.
    var liveUsers = new List<Stream>();
    string[] twitchIdsArray = userIds.ToArray();
    for (int i = 0; i < twitchIdsArray.Length; i += 100) {
      int lastIndex = i + 100;
      if (lastIndex > twitchIdsArray.Length) {
        lastIndex = twitchIdsArray.Length;
      }

      GetStreamsResponse? response =
        await api.Helix.Streams.GetStreamsAsync(userIds: twitchIdsArray[i..lastIndex].ToList());
      if (null != response) {
        liveUsers.AddRange(response.Streams.Where(s =>
          "live".Equals(s.Type, StringComparison.InvariantCultureIgnoreCase)));
      }
    }

    return liveUsers.Select(l => l.UserId);
  }

  /// <inheritdoc />
  public async Task<IEnumerable<Moderator>> GetChannelMods(string channelId, CancellationToken token = new()) {
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

  /// <inheritdoc />
  public async Task<bool> AddChannelMod(string channelId, string userId, CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();
      await api.Helix.Moderation.AddChannelModeratorAsync(channelId, userId);
      return true;
    }, Retries, token);
  }

  /// <summary>
  ///   Gets a new instance of the <see cref="TwitchAPI" />.
  /// </summary>
  /// <returns>A new instance of the <see cref="TwitchAPI" />.</returns>
  private TwitchAPI GetApi() {
    var api = new TwitchAPI {
      Settings = {
        ClientId = ClientId,
        AccessToken = OAuth?.AccessToken
      }
    };
    return api;
  }
}