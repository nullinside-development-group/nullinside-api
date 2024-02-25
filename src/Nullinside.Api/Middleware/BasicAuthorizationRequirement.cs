using Microsoft.AspNetCore.Authorization;

namespace Nullinside.Api.Middleware;

/// <summary>
///   Represents a requirement where a user is expected to have one role.
/// </summary>
public class BasicAuthorizationRequirement : IAuthorizationRequirement {
  /// <summary>
  ///   Initializes a new instance of the <see cref="BasicAuthorizationRequirement" /> class.
  /// </summary>
  /// <param name="role">The required role.</param>
  public BasicAuthorizationRequirement(string role) {
    Role = role;
  }

  /// <summary>
  ///   Gets or sets the required role.
  /// </summary>
  public string Role { get; set; }
}