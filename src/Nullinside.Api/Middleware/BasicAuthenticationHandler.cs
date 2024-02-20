using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Nullinside.Api.Model;
using Nullinside.Api.Model.Model;

namespace Nullinside.Api.Middleware;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
  private readonly NullinsideContext _dbContext;
  private readonly ILogger<BasicAuthenticationHandler> _logger;

  public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, NullinsideContext dbContext) : base(options, logger, encoder) {
    _dbContext = dbContext;
    _logger = logger.CreateLogger<BasicAuthenticationHandler>();
  }

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
    // Read token from HTTP request header
    string authorizationHeader = Request.Headers["Authorization"]!;
    if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ")) {
      return AuthenticateResult.Fail("No token");
    }

    // Remove "Bearer" to get pure token data
    string token = authorizationHeader.Substring("Bearer ".Length);

    User? dbUser;
    try {
      dbUser = await _dbContext.Users
        .Include(i => i.Roles)
        .AsNoTracking()
        .FirstOrDefaultAsync(u => !string.IsNullOrWhiteSpace(u.Token) &&
                                  u.Token.Equals(token, StringComparison.InvariantCultureIgnoreCase));

      if (null == dbUser) {
        return AuthenticateResult.Fail("Invalid token");
      }
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to verify token against database");
      return AuthenticateResult.Fail("Internal server error verifying token");
    }

    try {
      //auth logic
      List<Claim> claims = new() {
        new Claim(ClaimTypes.Email, dbUser.Gmail ?? string.Empty),
        new Claim(ClaimTypes.UserData, dbUser.Id.ToString())
      };

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
      _logger.LogError(ex, "Failed to create an auth ticket after successful token validation");
      return AuthenticateResult.Fail(ex);
    }
  }
}