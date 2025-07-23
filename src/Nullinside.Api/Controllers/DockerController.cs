using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
using Nullinside.Api.Common.Docker;
using Nullinside.Api.Common.Docker.Support;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Shared.Json;

namespace Nullinside.Api.Controllers;

/// <summary>
///   Provides insights and management options for the virtual machines hosted by the website.
/// </summary>
[Authorize(nameof(UserRoles.VM_ADMIN))]
[ApiController]
[Route("[controller]")]
public class DockerController : ControllerBase {
  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The docker proxy.
  /// </summary>
  private readonly IDockerProxy _docker;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _logger = LogManager.GetLogger(typeof(DockerController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="DockerController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  /// <param name="dockerProxy">The docker proxy.</param>
  public DockerController(INullinsideContext dbContext, IDockerProxy dockerProxy) {
    _dbContext = dbContext;
    _docker = dockerProxy;
  }

  /// <summary>
  ///   Gets the docker resources that can be configured.
  /// </summary>
  [Authorize(nameof(UserRoles.VM_ADMIN))]
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetDockerResources(CancellationToken token = new()) {
    // Get all existing docker containers and docker projects.
    Task<IEnumerable<DockerResource>> containers = _docker.GetContainers(token);
    Task<IEnumerable<DockerResource>> projects = _docker.GetDockerComposeProjects(token);

    // Get all known docker containers and docker projects that we want to expose to the site.
    Task<List<DockerDeployments>> recognizedProjects = _dbContext.DockerDeployments.ToListAsync(token);

    // Wait for all the async calls to finish.
    await Task.WhenAll(containers, projects, recognizedProjects).ConfigureAwait(false);

    // Map the output
    var response = new List<DockerResource>();
    foreach (DockerDeployments knownDeployment in recognizedProjects.Result) {
      DockerResource? existingContainer =
        knownDeployment.IsDockerComposeProject
          ? projects.Result.FirstOrDefault(c =>
            c.Name?.Equals(knownDeployment.Name, StringComparison.InvariantCultureIgnoreCase) ?? false)
          : containers.Result.FirstOrDefault(c =>
            c.Name?.Equals(knownDeployment.Name, StringComparison.InvariantCultureIgnoreCase) ?? false);
      response.Add(new DockerResource {
        Id = knownDeployment.Id,
        Name = knownDeployment.DisplayName,
        Notes = knownDeployment.Notes,
        IsOnline = existingContainer is { IsOnline: true }
      });
    }

    return Ok(response);
  }

  /// <summary>
  ///   Turns on or off the docker resource.
  /// </summary>
  /// <param name="id">The id of the docker resource.</param>
  /// <param name="request">The request to turn on or off a resource.</param>
  /// <param name="token">The cancellation token.</param>
  [Authorize(nameof(UserRoles.VM_ADMIN))]
  [HttpPost("{id:int}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> TurnOnOrOffDockerResources(int id, TurnOnOrOffDockerResourcesRequest request,
    CancellationToken token = new()) {
    DockerDeployments? recognizedProjects = await _dbContext.DockerDeployments
      .FirstOrDefaultAsync(d => d.Id == id, token).ConfigureAwait(false);
    if (null == recognizedProjects) {
      return BadRequest(new BasicServerFailure("'id' is invalid"));
    }

    bool result;
    if (recognizedProjects.IsDockerComposeProject) {
      result = await _docker.TurnOnOffDockerCompose(recognizedProjects.Name, request.TurnOn, token,
        recognizedProjects.ServerDir).ConfigureAwait(false);
    }
    else {
      result = await _docker.TurnOnOffDockerContainer(recognizedProjects.Name, request.TurnOn, token).ConfigureAwait(false);
    }

    return Ok(result);
  }
}