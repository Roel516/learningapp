namespace LearningResourcesApp.Client.Models.Authenticatie;

public class ValidationResult
{
    public bool IsGeldig { get; init; }
    public string Foutmelding { get; init; } = string.Empty;

    public static ValidationResult Success() => new() { IsGeldig = true };

    public static ValidationResult Failure(string foutmelding) => new()
    {
        IsGeldig = false,
        Foutmelding = foutmelding
    };
}
