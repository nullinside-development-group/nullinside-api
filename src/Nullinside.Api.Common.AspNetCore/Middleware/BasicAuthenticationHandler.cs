using System.Security.Claims;
using System.Text.Encodings.Web;

using log4net;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Common.AspNetCore.Middleware;

/// <summary>
///   Handles incoming Bearer tokens and converts them into objects that represents the user and their roles in the app.
/// </summary>
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _logger = LogManager.GetLogger(typeof(BasicAuthenticationHandler));

  /// <summary>
  ///   Initializes a new instance of the <see cref="BasicAuthenticationHandler" /> class.
  /// </summary>
  /// <param name="options">The options.</param>
  /// <param name="logger">The logger.</param>
  /// <param name="encoder">The url encoder.</param>
  /// <param name="dbContext">The database.</param>
  public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, INullinsideContext dbContext) : base(options, logger, encoder) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Pulls the bearer token out of the "Authorization" header and converts it into an object containing the user's
  ///   information and their roles.
  /// </summary>
  /// <returns>The user and their roles if successful, <see cref="AuthenticateResult.Fail(string)" /> otherwise.</returns>
  protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
    // Read token from HTTP request header
    string? authorizationHeader = Request.Headers.Authorization;
    if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ")) {
      return AuthenticateResult.Fail("No token");
    }

    // Remove "Bearer" to get pure token data
    string token = authorizationHeader["Bearer ".Length..];

    User? dbUser;
    try {
      dbUser = await _dbContext.Users
        .Include(i => i.Roles)
        .AsNoTracking()
        .FirstOrDefaultAsync(u => !string.IsNullOrWhiteSpace(u.Token) &&
                                  u.Token == token &&
                                  !u.IsBanned).ConfigureAwait(false);

      if (null == dbUser) {
        return AuthenticateResult.Fail("Invalid token");
      }
    }
    catch (Exception ex) {
      _logger.Error("Failed to verify token against database", ex);
      return AuthenticateResult.Fail("Internal server error verifying token");
    }

    try {
      //auth logic
      List<Claim> claims = [
        new(ClaimTypes.Email, dbUser.Email ?? string.Empty),
        new(ClaimTypes.UserData, dbUser.Id.ToString())
      ];

      if (null != dbUser.Roles) {
        foreach (UserRole role in dbUser.Roles) {
          claims.Add(new Claim(ClaimTypes.Role, role.Role.ToString()));
        }
      }

      ClaimsIdentity identity = new(claims, "BasicBearerToken");
      ClaimsPrincipal user = new(identity);
      AuthenticationProperties authProperties = new() {
        IsPersistent = true
      };

      AuthenticationTicket ticket = new(user, authProperties, "BasicBearerToken");
      return AuthenticateResult.Success(ticket);
    }
    catch (Exception ex) {
      _logger.Error("Failed to create an auth ticket after successful token validation", ex);
      return AuthenticateResult.Fail(ex);
    }
  }
}