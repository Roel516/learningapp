namespace LearningResourcesApp.Models.Auth;

public class Gebruiker
{
    public string Id { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsInterneMedewerker { get; set; }
}
