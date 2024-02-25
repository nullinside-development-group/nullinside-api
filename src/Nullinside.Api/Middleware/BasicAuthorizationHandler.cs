using Microsoft.AspNetCore.Authorization;

namespace Nullinside.Api.Middleware;

/// <summary>
///   Performs a basic check on the user's roles to ensure the user is in the role required for the endpoint.
/// </summary>
public class BasicAuthorizationHandler : AuthorizationHandler<BasicAuthorizationRequirement>, IAuthorizationRequirement {
  /// <summary>
  ///   Performs a basic check on the user's roles to ensure the user is in the role required for the endpoint.
  /// </summary>
  /// <param name="context">The user's information.</param>
  /// <param name="requirement">The role the user is expected to be in.</param>
  /// <returns>Nothing.</returns>
  protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BasicAuthorizationRequirement requirement) {
    try {
      // do logic
      if (context.User.IsInRole(requirement.Role)) {
        context.Succeed(requirement);
        return Task.CompletedTask;
      }

      context.Fail(new AuthorizationFailureReason(this, "User does not have permissions to this resource"));
      return Task.CompletedTask;
    }
    catch (Exception ex) {
      // handle error
      context.Fail(new AuthorizationFailureReason(this, ex.Message));
    }

    context.Fail();
    return Task.CompletedTask;
  }
}