using Microsoft.AspNetCore.Authorization;

namespace RelayHR.Web.Authorization;

public static class AuthorizationPolicies
{
    public const string ManageEmployeeExits = "ManageEmployeeExits";
    public const string ViewChecklists = "ViewChecklists";
    public const string ManageExpressVacancies = "ManageExpressVacancies";
    public const string SearchProfessionals = "SearchProfessionals";
    public const string RequestServices = "RequestServices";
    public const string RespondToRequests = "RespondToRequests";
    public const string ViewOwnRequests = "ViewOwnRequests";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManageEmployeeExits, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("HRAdmin"));

        options.AddPolicy(ViewChecklists, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("HRAdmin"));

        options.AddPolicy(ManageExpressVacancies, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("HRAdmin", "Recruiter"));

        options.AddPolicy(SearchProfessionals, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("Company"));

        options.AddPolicy(RequestServices, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("Company"));

        options.AddPolicy(RespondToRequests, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("Freelancer"));

        options.AddPolicy(ViewOwnRequests, policy =>
            policy.RequireAuthenticatedUser()
                .RequireRole("Company", "Freelancer"));
    }
}