using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

using Google.Apis.Auth;

using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

using Nullinside.Api.Common.Auth;
using Nullinside.Api.Common.Extensions;
using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Model.Shared;
using Nullinside.Api.Shared;
using Nullinside.Api.Shared.Json;

using Org.BouncyCastle.Utilities.Encoders;

namespace Nullinside.Api.Controllers;

/// <summary>
///   Handles user authentication and authorization.
/// </summary>
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase {
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
  private readonly ILog _logger = LogManager.GetLogger(typeof(UserController));

  /// <summary>
  ///   A collection of web sockets key'd by an id representing the request for the information.
  /// </summary>
  private readonly IWebSocketPersister _webSockets;

  /// <summary>
  ///   Initializes a new instance of the <see cref="UserController" /> class.
  /// </summary>
  /// <param name="configuration">The application's configuration file.</param>
  /// <param name="dbContext">The nullinside database.</param>
  /// <param name="webSocketPersister">The web socket persistence service.</param>
  public UserController(IConfiguration configuration, INullinsideContext dbContext, IWebSocketPersister webSocketPersister) {
    _configuration = configuration;
    _dbContext = dbContext;
    _webSockets = webSocketPersister;
  }

  /// <summary>
  ///   **NOT CALLED BY SITE OR USERS** This endpoint is called by google as part of their OpenId workflow. It
  ///   redirects users back to the nullinside website.
  /// </summary>
  /// <param name="creds">The credentials provided by google.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>A redirect to the nullinside website.</returns>
  [AllowAnonymous]
  [HttpPost]
  [Route("login")]
  public async Task<RedirectResult> Login([FromForm] GoogleOpenIdToken creds, CancellationToken token = new()) {
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    try {
      GoogleJsonWebSignature.Payload? credentials = await GenerateUserObject(creds).ConfigureAwait(false);
      if (string.IsNullOrWhiteSpace(credentials?.Email)) {
        return Redirect($"{siteUrl}/user/login?error=1");
      }

      var bearerToken = await UserHelpers.GenerateTokenAndSaveToDatabase(_dbContext, credentials.Email, Constants.OAUTH_TOKEN_TIME_LIMIT, cancellationToken: token).ConfigureAwait(false);
      if (null == bearerToken) {
        return Redirect($"{siteUrl}/user/login?error=2");
      }
      
      var json = JsonConvert.SerializeObject(bearerToken);
      return Redirect($"{siteUrl}/user/login?token={Convert.ToBase64String(Encoding.UTF8.GetBytes(json))}");
    }
    catch (InvalidJwtException) {
      return Redirect($"{siteUrl}/user/login?error=1");
    }
  }
  
  /// <summary>
  ///   Called to generate a new oauth token using the refresh token we previously provided.
  /// </summary>
  /// <param name="token">The refresh token we provided.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A redirect to the nullinside website.</returns>
  [AllowAnonymous]
  [HttpPost]
  [Route("token/refresh")]
  public async Task<ActionResult> Refresh(AuthToken token, CancellationToken cancellationToken = new()) {
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == token.Token, cancellationToken).ConfigureAwait(false);
    if (null == user?.Email) {
      return Unauthorized();
    }
    
    var bearerToken = await UserHelpers.GenerateTokenAndSaveToDatabase(_dbContext, user.Email, Constants.OAUTH_TOKEN_TIME_LIMIT, cancellationToken: cancellationToken).ConfigureAwait(false);
    if (null == bearerToken) {
      return StatusCode(500);
    }
    
    return Ok(bearerToken);
  }

  /// <summary>
  ///   Converts the credential string we get from google to a representation we read information from.
  /// </summary>
  /// <param name="creds">The credentials from Google.</param>
  /// <returns>The user information object.</returns>
  protected virtual async Task<GoogleJsonWebSignature.Payload?> GenerateUserObject(GoogleOpenIdToken creds) {
    return await GoogleJsonWebSignature.ValidateAsync(creds.credential).ConfigureAwait(false);
  }

  /// <summary>
  ///   **NOT CALLED BY SITE OR USERS** This endpoint is called by twitch as part of their oauth workflow. It
  ///   redirects users back to the nullinside website.
  /// </summary>
  /// <param name="code">The credentials provided by twitch.</param>
  /// <param name="api">The twitch api.</param>
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
  [Route("twitch-login")]
  public async Task<RedirectResult> TwitchLogin([FromQuery] string code, [FromServices] ITwitchApiProxy api,
    CancellationToken token = new()) {
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    if (null == await api.CreateAccessToken(code, token).ConfigureAwait(false)) {
      return Redirect($"{siteUrl}/user/login?error=3");
    }

    string? email = await api.GetUserEmail(token).ConfigureAwait(false);
    if (string.IsNullOrWhiteSpace(email)) {
      return Redirect($"{siteUrl}/user/login?error=4");
    }

    var bearerToken = await UserHelpers.GenerateTokenAndSaveToDatabase(_dbContext, email, Constants.OAUTH_TOKEN_TIME_LIMIT, cancellationToken: token).ConfigureAwait(false);
    if (null == bearerToken) {
      return Redirect($"{siteUrl}/user/login?error=2");
    }

    var json = JsonConvert.SerializeObject(bearerToken);
    return Redirect($"{siteUrl}/user/login?token={Convert.ToBase64String(Encoding.UTF8.GetBytes(json))}");
  }

  /// <summary>
  ///   **NOT CALLED BY SITE OR USERS** This endpoint is called by twitch as part of their oauth workflow. It
  ///   redirects users back to the nullinside website.
  /// </summary>
  /// <param name="code">The credentials provided by twitch.</param>
  /// <param name="state">An identifier for the request allowing for retrieval of the login information.</param>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  /// <returns>
  ///   A redirect to the nullinside website.
  ///   Errors:
  ///   1 = Internal error
  ///   2 = Error with twitch
  /// </returns>
  [AllowAnonymous]
  [HttpGet]
  [Route("twitch-login/twitch-streaming-tools")]
  public async Task<RedirectResult> TwitchStreamingToolsLogin([FromQuery] string code, [FromQuery] string state, [FromServices] ITwitchApiProxy api,
    CancellationToken token = new()) {
    // The first thing we need to do is make sure someone subscribed to a web socket waiting for the answer to the
    // credentials question we're being asked.
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    if (!_webSockets.WebSockets.ContainsKey(state)) {
      return Redirect($"{siteUrl}/user/login/desktop?error=1");
    }

    // Since someone already warned us this request was coming, create an oauth token from the code we received.
    if (null == await api.CreateAccessToken(code, token).ConfigureAwait(false)) {
      return Redirect($"{siteUrl}/user/login/desktop?error=2");
    }

    // The "someone" that warned us this request was coming has been sitting around waiting for an answer on a web
    // socket so we will pull up that socket and give them their oauth information. 
    try {
      WebSocket socket = _webSockets.WebSockets[state];
      var oAuth = new OAuthToken {
        AccessToken = api.OAuth?.AccessToken ?? string.Empty,
        RefreshToken = api.OAuth?.RefreshToken ?? string.Empty,
        ExpiresUtc = api.OAuth?.ExpiresUtc ?? DateTime.MinValue
      };

      await socket.SendTextAsync(JsonConvert.SerializeObject(oAuth), token).ConfigureAwait(false);
      await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Completed Successfully!", token).ConfigureAwait(false);
      _webSockets.WebSockets.TryRemove(state, out _);
      socket.Dispose();
    }
    catch {
      return Redirect($"{siteUrl}/user/login/desktop?error=1");
    }

    return Redirect($"{siteUrl}/user/login/desktop");
  }

  /// <summary>
  ///   A websocket used by clients to wait for their login token after twitch authenticates.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  [AllowAnonymous]
  [HttpGet]
  [Route("twitch-login/twitch-streaming-tools/ws")]
  public async Task TwitchStreamingToolsRefreshToken(CancellationToken token = new()) {
    if (HttpContext.WebSockets.IsWebSocketRequest) {
      // Connect with the client
      using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

      // The first communication over the web socket is always the id that we will later get from the
      // twitch api with the associated credentials.
      string id = await webSocket.ReceiveTextAsync(token).ConfigureAwait(false);
      id = id.Trim();

      // Add the web socket to web socket persistant service. It will be sitting there until the twitch api calls our
      // api later on.
      _webSockets.WebSockets.TryAdd(id, webSocket);

      // Regardless of whether you have a using statement above, the minute we leave the controller method we will
      // lose the connection. That's just the way web sockets are implemented in .NET Core Web APIs. So we have to sit
      // here in an await (specifically in an await so we don't mess up the thread pool) until twitch calls us.
      while (null == webSocket.CloseStatus) {
        await Task.Delay(1000, token).ConfigureAwait(false);
      }
    }
    else {
      HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
  }

  /// <summary>
  ///   Used to refresh OAuth tokens from the desktop application.
  /// </summary>
  /// <param name="refreshToken">The oauth refresh token provided by twitch.</param>
  /// <param name="api">The twitch api.</param>
  /// <param name="token">The cancellation token.</param>
  [AllowAnonymous]
  [HttpPost]
  [Route("twitch-login/twitch-streaming-tools")]
  public async Task<IActionResult> TwitchStreamingToolsRefreshToken([FromForm] string refreshToken, [FromServices] ITwitchApiProxy api,
    CancellationToken token = new()) {
    api.OAuth = new OAuthToken {
      AccessToken = null,
      RefreshToken = refreshToken,
      ExpiresUtc = DateTime.MinValue
    };

    if (null == await api.RefreshAccessToken(token).ConfigureAwait(false)) {
      return BadRequest();
    }

    return Ok(new OAuthToken {
      AccessToken = api.OAuth.AccessToken ?? string.Empty,
      RefreshToken = api.OAuth.RefreshToken ?? string.Empty,
      ExpiresUtc = api.OAuth.ExpiresUtc ?? DateTime.MinValue
    });
  }

  /// <summary>
  ///   Gets the roles of the current user.
  /// </summary>
  /// <returns>The collection of the user's roles.</returns>
  [HttpGet]
  [Route("roles")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public ObjectResult GetRoles() {
    return Ok(new {
      roles =
        (from identify in User.Identities
          from claim in identify.Claims
          where claim.Type == ClaimTypes.Role
          select claim.Value).Distinct().ToList()
    });
  }

  /// <summary>
  ///   Validates that the provided token is valid.
  /// </summary>
  /// <param name="token">The token to validate.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>200 if successful, 401 otherwise.</returns>
  [AllowAnonymous]
  [HttpPost]
  [Route("token/validate")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Validate(AuthToken token, CancellationToken cancellationToken = new()) {
    try {
      User? existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Token == token.Token && !u.IsBanned, cancellationToken).ConfigureAwait(false);
      if (null == existing) {
        return Unauthorized();
      }

      return Ok(true);
    }
    catch (Exception) {
      return StatusCode(500);
    }
  }
}