using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Auth;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Shared.Json;

using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
#if RELEASE
using System.Net;
using System.Net.Mail;
#endif

namespace Nullinside.Api.Controllers;

/// <summary>
///   Provides the ability to pull data from twitch.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TwitchController : ControllerBase {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchController));

  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _logger = LogManager.GetLogger(typeof(DockerController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public TwitchController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Retrieves all currently live individuals on twitch.
  /// </summary>
  [AllowAnonymous]
  [HttpGet("live")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetAllLiveBotStreams([FromServices] ITwitchApiProxy api, CancellationToken token = new()) {
    User? botUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.TwitchId == Constants.BOT_ID, token).ConfigureAwait(false);
    if (null == botUser || null == botUser.TwitchId || null == api.TwitchAppConfig?.ClientId || null == api.TwitchAppConfig?.ClientSecret) {
      return Problem("Internal error reaching out to twitch");
    }

    api.OAuth = new OAuthToken {
      AccessToken = botUser.TwitchToken,
      RefreshToken = botUser.TwitchRefreshToken,
      ExpiresUtc = botUser.TwitchTokenExpiration
    };

    DateTime earliestScan = DateTime.UtcNow.AddHours(-1);
#if DEBUG
    earliestScan = DateTime.UtcNow.AddDays(-365);
#endif

    // Get all users that have the bot enabled.
    List<User> users = await _dbContext.Users
      .Include(u => u.TwitchConfig)
      .Where(u =>
        !u.IsBanned &&
        null != u.TwitchConfig &&
        u.TwitchConfig.Enabled &&
        !string.IsNullOrWhiteSpace(u.TwitchId) &&
        u.TwitchLastScanned > earliestScan)
      .ToListAsync(token)
      .ConfigureAwait(false);
    if (users.Count == 0) {
      return Ok(Enumerable.Empty<TwitchLiveUsersResponse>());
    }
    
    // Ensure those channels have the bot modded
    var channels = (await api.GetUserModChannels(botUser.TwitchId).ConfigureAwait(false)).Select(c => c.broadcaster_id).ToList();
    users = users.Where(u => channels.Contains(u.TwitchId!)).ToList();

    List<string> liveUserIds = (await api.GetChannelsLive(users.Select(u => u.TwitchId).ToList()!).ConfigureAwait(false)).ToList();
    if (liveUserIds.Count == 0) {
      return Ok(Enumerable.Empty<TwitchLiveUsersResponse>());
    }

    List<User> liveUsers = users.Where(u => liveUserIds.Contains(u.TwitchId!)).ToList();
    if (liveUsers.Count == 0) {
      return Ok(Enumerable.Empty<TwitchLiveUsersResponse>());
    }

    IEnumerable<Stream>? twitchLiveUserInfo = await api.GetStreams(liveUsers.Where(l => null != l.TwitchId).Select(l => l.TwitchId).ToList()!, token: token).ConfigureAwait(false);
    if (null == twitchLiveUserInfo) {
      return Ok(Enumerable.Empty<TwitchLiveUsersResponse>());
    }

    return Ok(twitchLiveUserInfo.Select(u => new TwitchLiveUsersResponse(u)).ToList());
  }
}