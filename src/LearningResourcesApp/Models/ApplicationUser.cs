using Microsoft.AspNetCore.Identity;

namespace LearningResourcesApp.Models;

public class ApplicationUser : IdentityUser
{
    public string? Naam { get; set; }
}
