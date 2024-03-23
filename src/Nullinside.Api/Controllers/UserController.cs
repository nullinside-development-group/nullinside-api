using System.Security.Claims;
using System.Security.Cryptography;

using Google.Apis.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Shared;
using Nullinside.Api.Shared.Json;

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
  private readonly NullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILogger<UserController> _logger;

  /// <summary>
  ///   Initializes a new instance of the <see cref="UserController" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  /// <param name="configuration">The application's configuration file.</param>
  /// <param name="dbContext">The nullinside database.</param>
  public UserController(ILogger<UserController> logger, IConfiguration configuration, NullinsideContext dbContext) {
    _logger = logger;
    _configuration = configuration;
    _dbContext = dbContext;
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
  public async Task<IActionResult> Login([FromForm] GoogleOpenIdToken creds, CancellationToken token) {
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    try {
      GoogleJsonWebSignature.Payload? credentials = await GoogleJsonWebSignature.ValidateAsync(creds.credential);
      if (string.IsNullOrWhiteSpace(credentials?.Email)) {
        return Redirect($"{siteUrl}/user/login?error=1");
      }

      string? bearerToken = await GetTokenAndSaveToDatabase(credentials.Email, token);
      if (string.IsNullOrWhiteSpace(bearerToken)) {
        return Redirect($"{siteUrl}/user/login?error=2");
      }

      return Redirect($"{siteUrl}/user/login?token={bearerToken}");
    }
    catch (InvalidJwtException) {
      return Redirect($"{siteUrl}/user/login?error=1");
    }
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
  [Route("twitch-login")]
  public async Task<IActionResult> TwitchLogin([FromQuery] string code, CancellationToken token) {
    string? siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
    var api = new TwitchApiProxy();
    if (!await api.GetAccessToken(code, token)) {
      return Redirect($"{siteUrl}/user/login?error=3");
    }

    string? email = await api.GetUserEmail(token);
    if (string.IsNullOrWhiteSpace(email)) {
      return Redirect($"{siteUrl}/user/login?error=4");
    }

    string? bearerToken = await GetTokenAndSaveToDatabase(email, token);
    if (string.IsNullOrWhiteSpace(bearerToken)) {
      return Redirect($"{siteUrl}/user/login?error=2");
    }

    return Redirect($"{siteUrl}/user/login?token={bearerToken}");
  }

  /// <summary>
  ///   Generates a new bearer token, saves it to the database, and returns it.
  /// </summary>
  /// <param name="email">The email address of the user, user will be created if they don't already exist.</param>
  /// <param name="token">The cancellation token.</param>
  /// <param name="authToken">The authorization token for twitch, if applicable.</param>
  /// <param name="refreshToken">The refresh token for twitch, if applicable.</param>
  /// <param name="expires">The expiration date of the token for twitch, if applicable.</param>
  /// <returns>The bearer token if successful, null otherwise.</returns>
  private async Task<string?> GetTokenAndSaveToDatabase(string email, CancellationToken token = new(), string? authToken = null, string? refreshToken = null, DateTime? expires = null) {
    string bearerToken = GenerateBearerToken();
    try {
      User? existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, token);
      if (null == existing) {
        _dbContext.Users.Add(new User {
          Email = email,
          Token = bearerToken,
          TwitchToken = authToken,
          TwitchRefreshToken = refreshToken,
          TwitchTokenExpiration = expires,
          UpdatedOn = DateTime.UtcNow,
          CreatedOn = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(token);

        existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, token);
        if (null == existing) {
          return null;
        }

        _dbContext.UserRoles.Add(new UserRole {
          Role = UserRoles.User,
          UserId = existing.Id,
          RoleAdded = DateTime.UtcNow
        });
      }
      else {
        existing.Token = bearerToken;
        existing.TwitchToken = authToken;
        existing.TwitchRefreshToken = refreshToken;
        existing.TwitchTokenExpiration = expires;
        existing.UpdatedOn = DateTime.UtcNow;
      }

      await _dbContext.SaveChangesAsync(token);
      return bearerToken;
    }
    catch {
      return null;
    }
  }

  /// <summary>
  ///   Gets the roles of the current user.
  /// </summary>
  /// <returns>The collection of the user's roles.</returns>
  [HttpGet]
  [Route("roles")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public IActionResult GetRoles() {
    return Ok(new {
      roles =
        (from identify in User.Identities
          from claim in identify.Claims
          where claim.Type == ClaimTypes.Role
          select claim.Value).Distinct()
    });
  }

  /// <summary>
  ///   Validates that the provided token is valid.
  /// </summary>
  /// <param name="token">The token to validate.</param>
  /// <returns>200 if successful, 401 otherwise.</returns>
  [AllowAnonymous]
  [HttpPost]
  [Route("token/validate")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Validate(AuthToken token) {
    try {
      User? existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Token == token.Token);
      if (null == existing) {
        return Unauthorized();
      }

      return Ok(true);
    }
    catch (Exception) {
      return StatusCode(500);
    }
  }

  /// <summary>
  ///   Generates a new unique bearer token.
  /// </summary>
  /// <returns>A bearer token.</returns>
  private static string GenerateBearerToken() {
    // This method is trash but it doesn't matter. We should be doing real OAuth tokens with expirations and
    // renewals. Right now nothing that exists on the site requires this level of sophistication.
    string allowed = "ABCDEFGHIJKLMONOPQRSTUVWXYZabcdefghijklmonopqrstuvwxyz0123456789";
    int strlen = 255; // Or whatever
    char[] randomChars = new char[strlen];

    for (int i = 0; i < strlen; i++) {
      randomChars[i] = allowed[RandomNumberGenerator.GetInt32(0, allowed.Length)];
    }

    return new string(randomChars);
  }
}