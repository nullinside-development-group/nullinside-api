using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Shared;
using Nullinside.Api.Shared.Support;

namespace Nullinside.Api.Controllers;

/// <summary>
///   Handles user authentication and authorization.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TwitchBotController : ControllerBase {
  /// <summary>
  ///   The application's configuration file.
  /// </summary>
  private readonly IConfiguration _configuration;

  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _logger = LogManager.GetLogger(typeof(TwitchBotController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="UserController" /> class.
  /// </summary>
  /// <param name="configuration">The application's configuration file.</param>
  /// <param name="dbContext">The nullinside database.</param>
  public TwitchBotController(IConfiguration configuration, INullinsideContext dbContext) {
    _configuration = configuration;
    _dbContext = dbContext;
  }

  /// <summary>
  ///   **NOT CALLED BY SITE OR USERS** This endpoint is called by twitch as part of their oauth workflow. It
  ///   redirects users back to the nullinside website.
  /// </summary>
  /// <param name="code">The credentials provided by twitch.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>
  ///   A redirect to the nullinside website.
  ///   Errors:
  ///   2 = Internal error generating token.
  ///   3 = Code was invalid
  ///   4 = Twitch account has no email
  /// </returns>
  [AllowAnonymous]
  [HttpGet]
  [Route("login")]
  public async Task<IActionResult> TwitchLogin([FromQuery] string code, CancellationToken token) {
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    var api = new TwitchApiProxy();
    if (!await api.GetAccessToken(code, token)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.TwitchErrorWithToken}");
    }

    string? email = await api.GetUserEmail(token);
    if (string.IsNullOrWhiteSpace(email)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.TwitchAccountHasNoEmail}");
    }

    (string? id, string? username) user = await api.GetTwitchUser(token);
    if (string.IsNullOrWhiteSpace(user.username) || string.IsNullOrWhiteSpace(user.id)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.InternalError}");
    }

    string? bearerToken = await UserHelpers.GetTokenAndSaveToDatabase(_dbContext, email, token, api.AccessToken,
      api.RefreshToken, api.ExpiresUtc, user.username, user.id);
    if (string.IsNullOrWhiteSpace(bearerToken)) {
      return Redirect($"{siteUrl}/twitch-bot/config?error={TwitchBotLoginErrors.InternalError}");
    }

    return Redirect($"{siteUrl}/twitch-bot/config?token={bearerToken}");
  }
}