using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RelayHR.Web.Authorization;
using Xunit;

namespace RelayHR.Tests;

public class AuthorizationPoliciesTests
{
    private static readonly string[] AllRoles =
    {
        "HRAdmin", "Recruiter", "Company", "Freelancer"
    };

    public static IEnumerable<object[]> PermissionCases()
    {
        var permissions = new Dictionary<string, string[]>
        {
            [AuthorizationPolicies.ManageEmployeeExits] =
                new[] { "HRAdmin" },

            [AuthorizationPolicies.ViewChecklists] =
                new[] { "HRAdmin" },

            [AuthorizationPolicies.ManageExpressVacancies] =
                new[] { "HRAdmin", "Recruiter" },

            [AuthorizationPolicies.SearchProfessionals] =
                new[] { "Company" },

            [AuthorizationPolicies.RequestServices] =
                new[] { "Company" },

            [AuthorizationPolicies.RespondToRequests] =
                new[] { "Freelancer" },

            [AuthorizationPolicies.ViewOwnRequests] =
                new[] { "Company", "Freelancer" }
        };

        foreach (var permission in permissions)
        {
            foreach (var role in AllRoles)
            {
                yield return new object[]
                {
                    permission.Key,
                    role,
                    permission.Value.Contains(role)
                };
            }
        }
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddAuthorization(
            AuthorizationPolicies.Configure);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreateUser(
        string? role,
        bool authenticated)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString())
        };

        if (role is not null)
        {
            claims.Add(new Claim("role", role));
        }

        var identity = new ClaimsIdentity(
            claims,
            authenticated ? "Test" : null,
            "sub",
            "role");

        return new ClaimsPrincipal(identity);
    }

    [Theory]
    [MemberData(nameof(PermissionCases))]
    public async Task Policy_matches_permission_matrix(
        string policy,
        string role,
        bool expected)
    {
        using var services = CreateServices();

        var authorization =
            services.GetRequiredService<IAuthorizationService>();

        var user = CreateUser(role, authenticated: true);

        var result = await authorization.AuthorizeAsync(
            user,
            resource: null,
            policyName: policy);

        Assert.Equal(expected, result.Succeeded);
    }

    [Theory]
    [MemberData(nameof(PermissionCases))]
    public async Task Policy_rejects_unauthenticated_identity(
        string policy,
        string role,
        bool expected)
    {
        using var services = CreateServices();

        var authorization =
            services.GetRequiredService<IAuthorizationService>();

        // Incluso si aparecen claims de rol, no está autenticado.
        var user = CreateUser(role, authenticated: false);

        var result = await authorization.AuthorizeAsync(
            user,
            resource: null,
            policyName: policy);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("UnknownRole")]
    public async Task Authenticated_user_without_valid_role_is_denied(
        string? role)
    {
        using var services = CreateServices();

        var authorization =
            services.GetRequiredService<IAuthorizationService>();

        var user = CreateUser(role, authenticated: true);

        foreach (var policy in PermissionCases()
                     .Select(test => (string)test[0])
                     .Distinct())
        {
            var result = await authorization.AuthorizeAsync(
                user,
                resource: null,
                policyName: policy);

            Assert.False(result.Succeeded);
        }
    }
}