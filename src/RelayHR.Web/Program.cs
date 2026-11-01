using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Services;
using Microsoft.AspNetCore.Identity;
using RelayHR.Web.Entities;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RelayHR.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(

builder.Configuration.GetConnectionString("DefaultConnection")  ?? throw new InvalidOperationException("No se encontró la conexión DefaultConnection.")));

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Issuer.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Audience.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 64)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 64 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,

            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.Zero,

            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;

                if (!Guid.TryParse(
                        principal?.FindFirst("sub")?.Value,
                        out var userId)
                    || !Guid.TryParse(
                        principal?.FindFirst("security_stamp")?.Value,
                        out var securityStamp))
                {
                    context.Fail("Token no válido.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

                var user = await db.Users
                    .AsNoTracking()
                    .Include(user => user.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                    .SingleOrDefaultAsync(
                        user => user.Id == userId,
                        context.HttpContext.RequestAborted);

                if (user is null
                    || !user.IsActive
                    || user.SecurityStamp != securityStamp)
                {
                    context.Fail("La sesión ya no es válida.");
                    return;
                }

                var currentRoles = user.UserRoles
                    .Select(userRole => userRole.Role.Name)
                    .ToHashSet(StringComparer.Ordinal);

                var tokenRoles = principal!.FindAll("role")
                    .Select(claim => claim.Value)
                    .ToHashSet(StringComparer.Ordinal);

                if (currentRoles.Count == 0
                    || !currentRoles.SetEquals(tokenRoles))
                {
                    context.Fail("Los permisos de la sesión han cambiado.");
                }
            },

            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.Headers["WWW-Authenticate"] = "Bearer";

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Debes iniciar sesión con un token válido.",
                    data = (object?)null
                });
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "No tienes permisos para acceder a esta funcionalidad.",
                    data = (object?)null
                });
            }
        };
    });

builder.Services.AddAuthorization(
    AuthorizationPolicies.Configure);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.Run();
