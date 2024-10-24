using Microsoft.AspNetCore.Mvc;

using Moq;

using Nullinside.Api.Common.Docker;
using Nullinside.Api.Common.Docker.Support;
using Nullinside.Api.Controllers;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Shared.Json;

namespace Nullinside.Api.Tests.Nullinside.Api.Controllers;

/// <summary>
///   Tests for the <see cref="DockerController" /> class
/// </summary>
public class DockerControllerTests : UnitTestBase {
  /// <summary>
  ///   The docker proxy.
  /// </summary>
  private Mock<IDockerProxy> _docker;

  /// <summary>
  ///   Add the docker proxy.
  /// </summary>
  public override void Setup() {
    base.Setup();
    _docker = new Mock<IDockerProxy>();
  }

  /// <summary>
  ///   Tests that given a list of docker projects from the database we can match it against the actively running
  ///   projects on the server.
  /// </summary>
  [Test]
  public async Task DatabaseMatchesCommandOutputSuccessfully() {
    // Create three entries in the database for what we allow people to see. All of these should be in the output.
    _db.DockerDeployments.AddRange(
      new DockerDeployments {
        Id = 1, DisplayName = "Good", IsDockerComposeProject = true, Name = "Good", Notes = "Should be in output"
      },
      new DockerDeployments {
        Id = 2, DisplayName = "Good without matching name", IsDockerComposeProject = true, Name = "NonMatchingName",
        Notes = "Should be in output"
      },
      new DockerDeployments {
        Id = 3, DisplayName = "Good non-compose", IsDockerComposeProject = false, Name = "Stuff",
        Notes = "Should be in output"
      }
    );
    await _db.SaveChangesAsync();

    // Create two entries "in the server" for what is actually running. We should only match on the "good" one. The bad
    // one is different enough that it shouldn't match.
    var compose = new List<DockerResource> {
      new() { Id = 1, IsOnline = true, Name = "Good", Notes = "Should be in output's IsOnline field" },
      new() { Id = 2, IsOnline = false, Name = "Bad", Notes = "Should not be in output" }
    };
    _docker.Setup(d => d.GetDockerComposeProjects(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(compose.AsEnumerable()));

    var containers = new List<DockerResource> {
      new() { Id = 3, IsOnline = true, Name = "doesn't match", Notes = "Should not be in output" }
    };
    _docker.Setup(d => d.GetContainers(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(containers.AsEnumerable()));

    // Make the call and ensure it's successful.
    var controller = new DockerController(_db, _docker.Object);
    ObjectResult obj = await controller.GetDockerResources();
    Assert.That(obj.StatusCode, Is.EqualTo(200));

    // There should be three results. One that was actively running "Good" and the others weren't actively running.
    var deployments = obj.Value as List<DockerResource>;
    Assert.That(deployments, Is.Not.Null);
    Assert.That(deployments.Count, Is.EqualTo(3));
    Assert.That(deployments.FirstOrDefault(d => d.Name == "Good")?.IsOnline, Is.True);
    Assert.That(deployments.FirstOrDefault(d => d.Name == "Good without matching name")?.IsOnline, Is.False);
    Assert.That(deployments.FirstOrDefault(d => d.Name == "Good non-compose")?.IsOnline, Is.False);
  }

  /// <summary>
  ///   Tests that turning on/off a compose project calls the correct thing.
  /// </summary>
  [Test]
  public async Task OnOffComposeProjectsWork() {
    // Create two entries in the database for what we allow people to adjust.
    _db.DockerDeployments.AddRange(
      new DockerDeployments {
        Id = 1, DisplayName = "Good", IsDockerComposeProject = true, Name = "Good", Notes = "Should be in output"
      },
      new DockerDeployments {
        Id = 2, DisplayName = "Bad", IsDockerComposeProject = true, Name = "Bad", Notes = "Should not be in output"
      }
    );
    await _db.SaveChangesAsync();

    // Only a call with "Good" will work.
    _docker.Setup(d =>
        d.TurnOnOffDockerCompose("Good", It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
      .Returns(() => Task.FromResult(true));

    // Make the call and ensure it's successful.
    var controller = new DockerController(_db, _docker.Object);
    ObjectResult obj =
      await controller.TurnOnOrOffDockerResources(1, new TurnOnOrOffDockerResourcesRequest { TurnOn = true });
    Assert.That(obj.StatusCode, Is.EqualTo(200));

    // Ensure we called the 3rd party API with a value of "Good" to turn on a compose.
    bool deployments = (bool)obj.Value;
    Assert.That(deployments, Is.True);
  }

  /// <summary>
  ///   Tests that turning on/off a container project calls the correct thing.
  /// </summary>
  [Test]
  public async Task OnOffContainerWork() {
    // Create two entries in the database for what we allow people to adjust.
    _db.DockerDeployments.AddRange(
      new DockerDeployments {
        Id = 1, DisplayName = "Good", IsDockerComposeProject = false, Name = "Good", Notes = "Should be in output"
      },
      new DockerDeployments {
        Id = 2, DisplayName = "Bad", IsDockerComposeProject = false, Name = "Bad", Notes = "Should not be in output"
      }
    );
    await _db.SaveChangesAsync();

    // Only a call with "Good" will work.
    _docker.Setup(d => d.TurnOnOffDockerContainer("Good", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(true));

    // Make the call and ensure it's successful.
    var controller = new DockerController(_db, _docker.Object);
    ObjectResult obj =
      await controller.TurnOnOrOffDockerResources(1, new TurnOnOrOffDockerResourcesRequest { TurnOn = true });
    Assert.That(obj.StatusCode, Is.EqualTo(200));

    // Ensure we called the 3rd party API with a value of "Good" to turn on a container.
    bool deployments = (bool)obj.Value;
    Assert.That(deployments, Is.True);
  }

  /// <summary>
  ///   Tests providing an invalid id will result in a HTTP bad request.
  /// </summary>
  [Test]
  public async Task InvalidIdIsBadRequest() {
    // Create two entries in the database for what we allow people to adjust.
    _db.DockerDeployments.Add(
      new DockerDeployments {
        Id = 2, DisplayName = "Bad", IsDockerComposeProject = false, Name = "Bad", Notes = "Should not be in output"
      }
    );
    await _db.SaveChangesAsync();

    // Only a call with "Good" will get true.
    _docker.Setup(d => d.TurnOnOffDockerContainer("Good", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult(true));

    // Make the call and ensure it's successful.
    var controller = new DockerController(_db, _docker.Object);
    ObjectResult obj =
      await controller.TurnOnOrOffDockerResources(1, new TurnOnOrOffDockerResourcesRequest { TurnOn = true });
    Assert.That(obj.StatusCode, Is.EqualTo(400));

    // Bad request is returned to user with a generic error message.
    Assert.That(obj.Value, Is.TypeOf<BasicServerFailure>());
  }
}