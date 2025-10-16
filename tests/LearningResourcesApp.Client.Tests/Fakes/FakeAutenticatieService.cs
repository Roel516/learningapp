using LearningResourcesApp.Client.Services.Interfaces;
using Microsoft.JSInterop;
using LearningResourcesApp.Models.Auth;

namespace LearningResourcesApp.Client.Tests.Fakes;

public class FakeAutenticatieService : IAutenticatieService
{
    private Gebruiker? _huidigeGebruiker;

    public event Action? AutenticatieGewijzigd;

    public Gebruiker? HuidigeGebruiker => _huidigeGebruiker;
    public bool IsIngelogd => _huidigeGebruiker != null;

    public void SetHuidigeGebruiker(Gebruiker? gebruiker)
    {
        _huidigeGebruiker = gebruiker;
        AutenticatieGewijzigd?.Invoke();
    }

    public Task Initialiseer()
    {
        return Task.CompletedTask;
    }

    public Task<AuthResult> Registreren(RegisterRequest request)
    {
        return Task.FromResult(AuthResult.Success());
    }

    public Task<AuthResult> Inloggen(LoginRequest request)
    {
        return Task.FromResult(AuthResult.Success());
    }

    public string GenereerGoogleLoginUrl(string clientId, string redirectUri)
    {
        return "https://fake-google-login.com";
    }

    public Task<AuthResult> VerwerkOAuthCallback(string idToken, string accessToken)
    {
        return Task.FromResult(AuthResult.Success());
    }

    public Task Uitloggen()
    {
        _huidigeGebruiker = null;
        AutenticatieGewijzigd?.Invoke();
        return Task.CompletedTask;
    }
}