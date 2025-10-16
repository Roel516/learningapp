using LearningResourcesApp.Models.Auth;

namespace LearningResourcesApp.Client.Services.Interfaces;

public interface IAutenticatieService
{
    event Action? AutenticatieGewijzigd;

    Gebruiker? HuidigeGebruiker { get; }
    bool IsIngelogd { get; }

    Task Initialiseer();
    Task<AuthResult> Registreren(RegisterRequest request);
    Task<AuthResult> Inloggen(LoginRequest request);
    string GenereerGoogleLoginUrl(string clientId, string redirectUri);
    Task<AuthResult> VerwerkOAuthCallback(string idToken, string accessToken);
    Task Uitloggen();
}
