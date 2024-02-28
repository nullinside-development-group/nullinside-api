using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common.Json;
using Nullinside.Api.Common.Support;
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
  ///   The docker proxy.
  /// </summary>
  private readonly IDockerProxy _docker;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILogger<DockerController> _logger;

  /// <summary>
  ///   Initializes a new instance of the <see cref="DockerController" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  /// <param name="dbContext">The nullinside database.</param>
  /// <param name="dockerProxy">The docker proxy.</param>
  public DockerController(ILogger<DockerController> logger, NullinsideContext dbContext, IDockerProxy dockerProxy) {
    _logger = logger;
    _dbContext = dbContext;
    _docker = dockerProxy;
  }

  /// <summary>
  ///   Gets the docker resources that can be configured.
  /// </summary>
  [Authorize(nameof(UserRoles.VmAdmin))]
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> GetDockerResources(CancellationToken token) {
    // Get all existing docker containers and docker projects.
    Task<IEnumerable<DockerResource>> containers = _docker.GetContainers(token);
    Task<IEnumerable<DockerResource>> projects = _docker.GetDockerComposeProjects(token);

    // Get all known docker containers and docker projects that we want to expose to the site.
    Task<List<DockerDeployments>> recognizedProjects = _dbContext.DockerDeployments.ToListAsync(token);

    // Wait for all the async calls to finish.
    await Task.WhenAll(containers, projects, recognizedProjects);

    // Map the output
    var response = new List<DockerResource>();
    foreach (DockerDeployments knownDeployment in recognizedProjects.Result) {
      DockerResource? existingContainer =
        knownDeployment.IsDockerComposeProject
          ? projects.Result.FirstOrDefault(c => c.Name?.Equals(knownDeployment.Name, StringComparison.InvariantCultureIgnoreCase) ?? false)
          : containers.Result.FirstOrDefault(c => c.Name?.Equals(knownDeployment.Name, StringComparison.InvariantCultureIgnoreCase) ?? false);
      if (null == existingContainer) {
        continue;
      }

      response.Add(new DockerResource(knownDeployment) {
        IsOnline = existingContainer.IsOnline
      });
    }

    return Ok(response);
  }

  /// <summary>
  ///   Turns on or off the docker resource.
  /// </summary>
  /// <param name="id">The id of the docker resource.</param>
  [Authorize(nameof(UserRoles.VmAdmin))]
  [HttpPost("{id:int}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> TurnOnOrOffDockerResources(int id, TurnOnOrOffDockerResourcesRequest request, CancellationToken token) {
    DockerDeployments? recognizedProjects = await _dbContext.DockerDeployments
      .FirstOrDefaultAsync(d => d.Id == id, token);
    if (null == recognizedProjects) {
      return BadRequest(new BasicServerFailure("'id' is invalid"));
    }

    bool result;
    if (recognizedProjects.IsDockerComposeProject) {
      result = await _docker.TurnOnOffDockerCompose(recognizedProjects.Name, request.TurnOn, token);
    }
    else {
      result = await _docker.TurnOnOffDockerContainer(recognizedProjects.Name, request.TurnOn, token);
    }

    return Ok(result);
  }
}