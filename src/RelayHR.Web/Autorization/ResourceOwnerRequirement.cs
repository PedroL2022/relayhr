using Microsoft.AspNetCore.Authorization;

namespace RelayHR.Web.Authorization;

public sealed class ResourceOwnerRequirement : IAuthorizationRequirement
{
}

// Los identificadores deben obtenerse del recurso guardado en la base de datos.
public sealed record ResourceOwners(IReadOnlyCollection<Guid> UserIds);

public sealed class ResourceOwnerHandler
    : AuthorizationHandler<ResourceOwnerRequirement, ResourceOwners>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement,
        ResourceOwners resource)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(
                context.User.FindFirst("sub")?.Value,
                out var userId)
            && resource.UserIds.Contains(userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}