using Microsoft.AspNetCore.Identity;

namespace LearningResourcesApp.Services.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(IdentityUser user);
}
