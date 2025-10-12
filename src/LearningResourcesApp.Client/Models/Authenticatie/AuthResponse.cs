namespace LearningResourcesApp.Client.Models.Authenticatie;

public record AuthResponse
{
    public bool Succes { get; init; }
    public string? Foutmelding { get; init; }
    public UserInfo? Gebruiker { get; init; }
}
