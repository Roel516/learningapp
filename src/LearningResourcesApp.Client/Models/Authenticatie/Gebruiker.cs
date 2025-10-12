namespace LearningResourcesApp.Client.Models.Authenticatie;

public class Gebruiker
{
    public string Id { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsIngelogd { get; set; }
    public bool IsInterneMedewerker { get; set; }
}
