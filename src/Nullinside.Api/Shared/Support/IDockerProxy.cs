namespace Nullinside.Api.Shared.Support;

/// <summary>
///   The contract to communicate with a docker server.
/// </summary>
public interface IDockerProxy {
  /// <summary>
  ///   Gets the list of docker containers.
  /// </summary>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A collection of the existing containers.</returns>
  Task<IEnumerable<DockerResource>> GetContainers(CancellationToken cancellationToken);

  /// <summary>
  ///   Gets the list of docker compose projects.
  /// </summary>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A collection of the existing docker compose projects.</returns>
  Task<IEnumerable<DockerResource>> GetDockerComposeProjects(CancellationToken cancellationToken);

  /// <summary>
  ///   Turns on or off the docker container.
  /// </summary>
  /// <param name="name">The container name.</param>
  /// <param name="turnOn">True to start the resource up, false to turn it off.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>True if successful, false otherwise.</returns>
  Task<bool> TurnOnOffDockerContainer(string name, bool turnOn, CancellationToken cancellationToken);

  /// <summary>
  ///   Turns on or off the docker container.
  /// </summary>
  /// <param name="name">The project name.</param>
  /// <param name="turnOn">True to start the resource up, false to turn it off.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>True if successful, false otherwise.</returns>
  Task<bool> TurnOnOffDockerCompose(string name, bool turnOn, CancellationToken cancellationToken);
}