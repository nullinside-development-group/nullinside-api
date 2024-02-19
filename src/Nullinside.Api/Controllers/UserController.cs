using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nullinside.Api.Common;

namespace Nullinside.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromForm] GoogleOpenIdToken creds)
    {
        try
        {
            var credentials = await GoogleJsonWebSignature.ValidateAsync(creds.credential);
        }
        catch (InvalidJwtException)
        {
            return Forbid();
        }

        var siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
        return Redirect($"{siteUrl}/google/login");
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("token/validate")]
    public async Task<IActionResult> Validate(AuthToken token)
    {
        // return Ok(true);
        return Unauthorized();
    }
}