namespace LearningResourcesApp.Models.Auth;

public record AuthResponse
{
    public bool Succes { get; init; }
    public string? Foutmelding { get; init; }
    public Gebruiker? Gebruiker { get; init; }
}
