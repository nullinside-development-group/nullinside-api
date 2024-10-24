using System.Diagnostics.CodeAnalysis;

namespace Nullinside.Api.Shared.Json;

/// <summary>
///   A request to turn on or off a docker resource.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "JSON")]
public class TurnOnOrOffDockerResourcesRequest {
  /// <summary>
  ///   True to turn the resource on, false to turn it off.
  /// </summary>
  public bool TurnOn { get; set; }
}