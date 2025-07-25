using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model;

namespace Nullinside.Api.Controllers;

/// <summary>
///   Handles user authentication and authorization.
/// </summary>
[ApiController]
[Route("[controller]")]
public class FeatureToggleController : ControllerBase {
  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _logger = LogManager.GetLogger(typeof(FeatureToggleController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="FeatureToggleController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public FeatureToggleController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Gets the feature toggles.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The collection of feature toggles.</returns>
  [AllowAnonymous]
  [HttpGet]
  public async Task<ObjectResult> GetAll(CancellationToken token = new()) {
    return Ok(await _dbContext.FeatureToggle.ToListAsync(token).ConfigureAwait(false));
  }
}