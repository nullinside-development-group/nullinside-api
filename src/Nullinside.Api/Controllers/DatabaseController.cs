using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model;

namespace Nullinside.Api.Controllers;

/// <summary>
/// Provides insights and management options for the internal database of the website.
/// </summary>
[ApiController]
[Route("[controller]")]
public class DatabaseController : ControllerBase {
  /// <summary>
  /// The nullinside database.
  /// </summary>
  private readonly NullinsideContext _dbContext;

  /// <summary>
  /// The logger.
  /// </summary>
  private readonly ILogger<DatabaseController> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="DatabaseController" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  /// <param name="dbContext">The nullinside database.</param>
  public DatabaseController(ILogger<DatabaseController> logger, NullinsideContext dbContext) {
    _logger = logger;
    _dbContext = dbContext;
  }

  /// <summary>
  /// Performs a database migration, apply any updates in a blocking call.
  /// </summary>
  /// <returns>True</returns>
  [AllowAnonymous]
  [HttpGet]
  [Route("migration")]
  public async Task<IActionResult> Migrate() {
    await _dbContext.Database.MigrateAsync();
    return Ok();
  }
}