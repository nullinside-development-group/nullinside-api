using System.Security.Cryptography;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nullinside.Api.Common;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Model;

namespace Nullinside.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly NullinsideContext _dbContext;
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger, IConfiguration configuration, NullinsideContext dbContext)
    {
        _logger = logger;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromForm] GoogleOpenIdToken creds)
    {
        var siteUrl = _configuration.GetValue<string>("Api:SiteUrl");
        string token;
        try
        {
            var credentials = await GoogleJsonWebSignature.ValidateAsync(creds.credential);
            if (string.IsNullOrWhiteSpace(credentials?.Email)) return Redirect($"{siteUrl}/google/login?error=1");

            token = GenerateBearerToken();
            try
            {
                var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Gmail == credentials.Email);
                if (null == existing)
                {
                    _dbContext.Users.Add(new User
                    {
                        Gmail = credentials.Email,
                        Token = token,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    });

                    await _dbContext.SaveChangesAsync();

                    existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Gmail == credentials.Email);
                    if (null == existing) return Redirect($"{siteUrl}/google/login?error=2");

                    _dbContext.UserRoles.Add(new UserRole
                    {
                        Role = UserRoles.USER,
                        UserId = existing.Id,
                        RoleAdded = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Token = token;
                    existing.UpdatedOn = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                return Redirect($"{siteUrl}/google/login?token={token}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get/create/update user token in database");
                return Redirect($"{siteUrl}/google/login?error=2");
            }
        }
        catch (InvalidJwtException)
        {
            return Redirect($"{siteUrl}/google/login?error=1");
        }
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("token/validate")]
    public async Task<IActionResult> Validate(AuthToken token)
    {
        try
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Token == token.Token);
            if (null == existing) return Unauthorized();

            return Ok(true);
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

    private static string GenerateBearerToken()
    {
        var allowed = "ABCDEFGHIJKLMONOPQRSTUVWXYZabcdefghijklmonopqrstuvwxyz0123456789";
        var strlen = 255; // Or whatever
        var randomChars = new char[strlen];

        for (var i = 0; i < strlen; i++) randomChars[i] = allowed[RandomNumberGenerator.GetInt32(0, allowed.Length)];

        return new string(randomChars);
    }
}