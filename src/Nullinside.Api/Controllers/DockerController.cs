using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Controllers;

/// <summary>
///   Provides insights and management options for the virtual machines hosted by the website.
/// </summary>
[Authorize(nameof(UserRoles.VmAdmin))]
[ApiController]
[Route("[controller]")]
public class DockerController : ControllerBase {
  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly NullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILogger<DockerController> _logger;

  /// <summary>
  ///   Initializes a new instance of the <see cref="DockerController" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  /// <param name="dbContext">The nullinside database.</param>
  public DockerController(ILogger<DockerController> logger, NullinsideContext dbContext) {
    _logger = logger;
    _dbContext = dbContext;
  }
}