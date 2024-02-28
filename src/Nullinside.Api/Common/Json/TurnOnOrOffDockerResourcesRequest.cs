namespace Nullinside.Api.Common.Json;

/// <summary>
///   A request to turn on or off a docker resource.
/// </summary>
public class TurnOnOrOffDockerResourcesRequest {
  /// <summary>
  ///   True to turn the resource on, false to turn it off.
  /// </summary>
  public bool TurnOn { get; set; }
}