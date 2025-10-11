using LearningResourcesApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace LearningResourcesApp.Authorization;

/// <summary>
/// Authorization handler that checks if the user has the InterneMedewerker claim.
/// </summary>
public class InterneMedewerkerHandler : AuthorizationHandler<InterneMedewerkerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InterneMedewerkerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var hasInterneMedewerkerClaim = context.User.HasClaim(
            c => c.Type == AppClaims.InterneMedewerker && c.Value == "true");

        if (hasInterneMedewerkerClaim)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
