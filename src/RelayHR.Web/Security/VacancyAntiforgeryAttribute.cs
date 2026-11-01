using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace RelayHR.Web.Security;
[AttributeUsage(AttributeTargets.Method)]
public sealed class VacancyAntiforgeryAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identities.Any(i => i.IsAuthenticated && i.AuthenticationType == IdentityConstants.ApplicationScheme)) return;
        try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException)
        { context.Result = new BadRequestObjectResult(RelayHR.Web.Dtos.ApiResponse<object>.Fail("Token antifalsificación inválido. Recargue la página.")); }
    }
}
