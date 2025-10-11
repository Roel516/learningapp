using Microsoft.AspNetCore.Authorization;

namespace LearningResourcesApp.Authorization;

/// <summary>
/// Authorization requirement for verifying a user is an internal employee (Interne Medewerker).
/// </summary>
public class InterneMedewerkerRequirement : IAuthorizationRequirement
{
}
