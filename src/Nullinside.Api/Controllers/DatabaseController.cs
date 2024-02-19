using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nullinside.Api.Model;

namespace Nullinside.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly NullinsideContext _dbContext;

    public DatabaseController(ILogger<UserController> logger, NullinsideContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("migration")]
    public async Task<IActionResult> Migrate()
    {
        await this._dbContext.Database.MigrateAsync();
        return Ok();
    }
}