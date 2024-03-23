namespace Nullinside.Api.Common.Docker.Support;

/// <summary>
///   The `docker compose ls --format 'json'` output.
/// </summary>
public class DockerComposeLsOutput {
  /// <summary>
  ///   The name of the docker compose project.
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  ///   The current status of the docker compose project.
  /// </summary>
  public string? Status { get; set; }
}