namespace LearningResourcesApp.Client.Models.Authenticatie;

public record UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Naam { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsInterneMedewerker { get; init; }
}
