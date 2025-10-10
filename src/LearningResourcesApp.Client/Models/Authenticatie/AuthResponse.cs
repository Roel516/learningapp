namespace LearningResourcesApp.Client.Models.Authenticatie;

public class AuthResponse
{
    public bool Succes { get; set; }
    public string? Foutmelding { get; set; }
    public UserInfo? Gebruiker { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsInterneMedewerker { get; set; }
}
