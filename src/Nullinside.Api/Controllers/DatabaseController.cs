using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nullinside.Api.Model;

namespace Nullinside.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly NullinsideContext _dbContext;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(ILogger<DatabaseController> logger, NullinsideContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("migration")]
    public async Task<IActionResult> Migrate()
    {
        await _dbContext.Database.MigrateAsync();
        return Ok();
    }
}