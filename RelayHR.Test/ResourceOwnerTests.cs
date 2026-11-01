using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RelayHR.Web.Authorization;
using Xunit;

namespace RelayHR.Tests;

public class ResourceOwnerTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public async Task Access_requires_authentication_and_ownership(
        bool authenticated,
        bool isOwner,
        bool expected)
    {
        var userId = Guid.NewGuid();

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim("sub", userId.ToString()) },
                authenticated ? "Test" : null));

        var owners = new ResourceOwners(new[]
        {
            isOwner ? userId : Guid.NewGuid()
        });

        var requirement = new ResourceOwnerRequirement();

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            owners);

        var handler = new ResourceOwnerHandler();

        await handler.HandleAsync(context);

        Assert.Equal(expected, context.HasSucceeded);
    }

    [Fact]
    public async Task Invalid_user_identifier_is_denied()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim("sub", "identificador-invalido") },
                "Test"));

        var requirement = new ResourceOwnerRequirement();

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            new ResourceOwners(new[] { Guid.NewGuid() }));

        await new ResourceOwnerHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}