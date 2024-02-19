using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Nullinside.Api.Common;

namespace Nullinside.Api.Middleware;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read token from HTTP request header
        string authorizationHeader = Request.Headers["Authorization"]!;
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            return Task.FromResult(AuthenticateResult.Fail("no token"));
        // Remove "Bearer" to get pure token data
        var token = authorizationHeader.Substring("Bearer ".Length);

        try
        {
            //auth logic
            var claims = new Claim[]
            {
                new(ClaimTypes.Email, "hello@gmail.com"),
                new(ClaimTypes.UserData, "1"),
                new(ClaimTypes.Role, AuthRoles.USER)
            };
            var identity = new ClaimsIdentity(claims, "BasicBearerToken");
            var user = new ClaimsPrincipal(identity);
            user.IsInRole(AuthRoles.USER);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            var ticket = new AuthenticationTicket(user, authProperties, "BasicBearerToken");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            //oops
            return Task.FromResult(AuthenticateResult.Fail(ex));
        }
    }
}