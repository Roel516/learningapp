namespace LearningResourcesApp.Models;

public class ExternalLoginRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;
}
