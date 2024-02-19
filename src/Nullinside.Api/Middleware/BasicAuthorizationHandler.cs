using Microsoft.AspNetCore.Authorization;

namespace Nullinside.Api.Middleware;

public class BasicAuthorizationHandler : AuthorizationHandler<BasicAuthorizationRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        BasicAuthorizationRequirement requirement)
    {
        try
        {
            // do logic
            if (context.User.IsInRole(requirement.Role))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            context.Fail(new AuthorizationFailureReason(this, "User does not have permissions to this resource"));
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // handle error
            context.Fail(new AuthorizationFailureReason(this, ex.Message));
        }

        context.Fail();
        return Task.CompletedTask;
    }
}