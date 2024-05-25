namespace Nullinside.Api.Common.Docker.Support;

/// <summary>
/// A docker resource representing either a docker compose project
/// or a single docker container.
/// </summary>
public class DockerResource {
  /// <summary>
  /// Initializes a new instance of the <see cref="DockerResource" /> class.
  /// </summary>
  public DockerResource() {
  }

  /// <summary>
  /// Gets or sets the unique identifier of the row.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  /// Gets or sets the display name.
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  /// Gets or sets the comment that should be shown on the screen in reference to the docker project/container.
  /// </summary>
  public string? Notes { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the docker resource is running.
  /// </summary>
  public bool IsOnline { get; set; }
}