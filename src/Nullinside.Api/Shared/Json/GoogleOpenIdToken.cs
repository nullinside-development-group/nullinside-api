using System.Diagnostics.CodeAnalysis;

namespace Nullinside.Api.Shared.Json;

/// <summary>
///   Represents the response from google for an OpenId token.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "JSON")]
public class GoogleOpenIdToken {
  /// <summary>
  ///   The cross site scripting check.
  /// </summary>
  public string? g_csrf_token { get; set; }

  /// <summary>
  ///   The credentials.
  /// </summary>
  public string? credential { get; set; }
}