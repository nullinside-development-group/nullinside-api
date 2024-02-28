using Newtonsoft.Json;

using Nullinside.Api.Common.Json;
using Nullinside.Api.Common.Support;

using Renci.SshNet;

namespace Nullinside.Api.Common;

/// <summary>
///   Handles interactions with docker on a docker server.
/// </summary>
public class DockerProxy : IDockerProxy {
  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILogger<DockerProxy> _logger;

  /// <summary>
  ///   The password for the docker server.
  /// </summary>
  private readonly string? _password = Environment.GetEnvironmentVariable("DOCKER_PASSWORD");

  /// <summary>
  ///   The password for the docker server.
  /// </summary>
  private readonly string? _password2 = Environment.GetEnvironmentVariable("DOCKER_PASSWORD2");

  /// <summary>
  ///   The server that hosts the docker deployments.
  /// </summary>
  private readonly string? _server = Environment.GetEnvironmentVariable("DOCKER_SERVER");

  /// <summary>
  ///   The username to login to the docker server.
  /// </summary>
  private readonly string? _username = Environment.GetEnvironmentVariable("DOCKER_USERNAME");

  /// <summary>
  ///   Initializes a new instance of the <see cref="DockerProxy" /> class.
  /// </summary>
  /// <param name="logger">The logger.</param>
  public DockerProxy(ILogger<DockerProxy> logger) {
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<IEnumerable<DockerResource>> GetContainers(CancellationToken cancellationToken) {
    (string output, string error) response = await ExecuteCommand("docker container ls -a --format '{{.Names}}|{{.Status}}'", cancellationToken);
    if (string.IsNullOrWhiteSpace(response.output)) {
      return Enumerable.Empty<DockerResource>();
    }

    var containers = new List<DockerResource>();
    foreach (string line in response.output.Split('\n')) {
      if (string.IsNullOrWhiteSpace(line)) {
        continue;
      }

      string[] parts = line.Split('|');
      if (parts.Length != 2) {
        _logger.LogError("Failed to parse the following docker container name into two parts on the '|' char: {line}", line);
        continue;
      }

      containers.Add(new DockerResource {
        Name = parts[0].Trim(),
        IsOnline = parts[1].Trim().StartsWith("Up", StringComparison.InvariantCultureIgnoreCase)
      });
    }

    return containers;
  }

  /// <inheritdoc />
  public async Task<IEnumerable<DockerResource>> GetDockerComposeProjects(CancellationToken cancellationToken) {
    (string output, string error) responseJson = await ExecuteCommand("docker compose ls -a --format 'json'", cancellationToken);
    if (string.IsNullOrWhiteSpace(responseJson.output)) {
      return Enumerable.Empty<DockerResource>();
    }

    var response = JsonConvert.DeserializeObject<List<DockerComposeLsOutput>>(responseJson.output);
    if (null == response) {
      return Enumerable.Empty<DockerResource>();
    }

    return response.Select(line => new DockerResource {
      Name = line.Name?.Trim(),
      IsOnline = line.Status?.Trim().StartsWith("running", StringComparison.InvariantCultureIgnoreCase) ?? false
    });
  }

  /// <inheritdoc />
  public async Task<bool> TurnOnOffDockerContainer(string name, bool turnOn, CancellationToken cancellationToken) {
    string command = turnOn ? "start" : "stop";
    (string output, string error) responseJson = await ExecuteCommand($"docker container {command} {name}", cancellationToken);
    if (string.IsNullOrWhiteSpace(responseJson.error)) {
      return false;
    }

    return (turnOn && responseJson.error.Contains("Started", StringComparison.InvariantCultureIgnoreCase)) ||
           (!turnOn && responseJson.error.Contains("Stopped", StringComparison.InvariantCultureIgnoreCase));
  }

  /// <inheritdoc />
  public async Task<bool> TurnOnOffDockerCompose(string name, bool turnOn, CancellationToken cancellationToken) {
    string command = turnOn ? "start" : "stop";
    (string output, string error) responseJson = await ExecuteCommand($"docker compose -p {name} {command}", cancellationToken);
    if (string.IsNullOrWhiteSpace(responseJson.error)) {
      return false;
    }

    return (turnOn && responseJson.error.Contains("Started", StringComparison.InvariantCultureIgnoreCase)) ||
           (!turnOn && responseJson.error.Contains("Stopped", StringComparison.InvariantCultureIgnoreCase));
  }

  private async Task<(string output, string error)> ExecuteCommand(string command, CancellationToken token) {
    using SshClient client = new(_server, _username, _password);
    await client.ConnectAsync(token);
    using SshCommand? responseJson = client.RunCommand($"echo {_password2} | sudo -S {command}");
    return (responseJson.Result, responseJson.Error);
  }
}