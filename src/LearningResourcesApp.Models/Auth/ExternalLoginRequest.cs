namespace LearningResourcesApp.Models.Auth;

public record ExternalLoginRequest
{
    public string Provider { get; init; } = string.Empty;
    public string ProviderId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Naam { get; init; } = string.Empty;
}
