namespace LearningResourcesApp.Models.Auth;

public record GebruikerInfoResult
{
    public bool IsGeldig { get; init; }
    public string Foutmelding { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Naam { get; init; } = string.Empty;
    public string GoogleId { get; init; } = string.Empty;

    public static GebruikerInfoResult Success(string email, string naam, string googleId) => new()
    {
        IsGeldig = true,
        Email = email,
        Naam = naam,
        GoogleId = googleId
    };

    public static GebruikerInfoResult Failure(string foutmelding) => new()
    {
        IsGeldig = false,
        Foutmelding = foutmelding
    };
}
