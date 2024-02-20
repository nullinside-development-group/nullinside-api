using Microsoft.AspNetCore.Authorization;

namespace Nullinside.Api.Middleware;

public class BasicAuthorizationRequirement : IAuthorizationRequirement {
  public BasicAuthorizationRequirement(string role) {
    Role = role;
  }

  public string Role { get; set; }
}