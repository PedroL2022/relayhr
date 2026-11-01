using Microsoft.AspNetCore.Identity;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public static class IdentityBootstrapSeeder
{
    public static async Task SeedConfiguredUserAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Identity:BootstrapUser");
        var name = section["Name"];
        var email = section["Email"];
        var password = section["Password"];

        // Sin secretos explícitos no se crea una cuenta predeterminada.
        if (string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Configure Identity:BootstrapUser:Name, Email y Password juntos.");
        }

        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new User
            {
                Name = name,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, "No se pudo crear el usuario inicial.");
        }

        if (!await userManager.IsInRoleAsync(user, IdentityRoles.HumanResources))
        {
            var roleResult = await userManager.AddToRoleAsync(user, IdentityRoles.HumanResources);
            EnsureSucceeded(roleResult, "No se pudo asignar el rol de Recursos Humanos.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"{message} {errors}");
        }
    }
}
