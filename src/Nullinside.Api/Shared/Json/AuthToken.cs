using System.Diagnostics.CodeAnalysis;

namespace Nullinside.Api.Shared.Json;

/// <summary>
///   Represents an authentication token provided to the site via a "Bearer" token header.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "JSON")]
public class AuthToken {
  /// <summary>
  ///   Initializes a new instance of the <see cref="AuthToken" /> class.
  /// </summary>
  /// <param name="token">The bearer token.</param>
  public AuthToken(string token) {
    Token = token;
  }

  /// <summary>
  ///   Gets or sets the authentication token provided to the site via a "Bearer" token header.
  /// </summary>
  public string Token { get; set; }
}