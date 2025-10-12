namespace LearningResourcesApp.Client.Models.Authenticatie;

public class AuthResult
{
    public bool Succes { get; init; }
    public string? Foutmelding { get; init; }

    public static AuthResult Success() => new() { Succes = true };

    public static AuthResult Failure(string foutmelding) => new()
    {
        Succes = false,
        Foutmelding = foutmelding
    };
}
