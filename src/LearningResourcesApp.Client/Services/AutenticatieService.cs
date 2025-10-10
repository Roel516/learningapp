using LearningResourcesApp.Client.Models.Authenticatie;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace LearningResourcesApp.Client.Services;

public class AutenticatieService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private Gebruiker? _huidigeGebruiker;
    private const string ApiBaseUrl = "api/account";

    public event Action? AutenticatieGewijzigd;

    public Gebruiker? HuidigeGebruiker => _huidigeGebruiker;

    public bool IsIngelogd => _huidigeGebruiker?.IsIngelogd ?? false;

    public AutenticatieService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    // Initialiseer authenticatie - controleer of gebruiker ingelogd is
    public async Task Initialiseer()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AuthResponse>($"{ApiBaseUrl}/current-user");
            if (response?.Succes == true && response.Gebruiker != null)
            {
                _huidigeGebruiker = new Gebruiker
                {
                    Id = response.Gebruiker.Id,
                    Naam = response.Gebruiker.Naam,
                    Email = response.Gebruiker.Email,
                    IsIngelogd = true,
                    IsInterneMedewerker = response.Gebruiker.IsInterneMedewerker
                };
                AutenticatieGewijzigd?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij initialiseren authenticatie: {ex.Message}");
        }
    }

    // Registreer met email/wachtwoord (Identity)
    public async Task<(bool Succes, string? Foutmelding)> Registreren(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Succes == true && result.Gebruiker != null)
            {
                _huidigeGebruiker = new Gebruiker
                {
                    Id = result.Gebruiker.Id,
                    Naam = result.Gebruiker.Naam,
                    Email = result.Gebruiker.Email,
                    IsIngelogd = true,
                    IsInterneMedewerker = result.Gebruiker.IsInterneMedewerker
                };
                AutenticatieGewijzigd?.Invoke();
                return (true, null);
            }

            return (false, result?.Foutmelding ?? "Registratie mislukt");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij registreren: {ex.Message}");
            return (false, "Er is een fout opgetreden bij registreren");
        }
    }

    // Login met email/wachtwoord (Identity)
    public async Task<(bool Succes, string? Foutmelding)> Inloggen(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Succes == true && result.Gebruiker != null)
            {
                _huidigeGebruiker = new Gebruiker
                {
                    Id = result.Gebruiker.Id,
                    Naam = result.Gebruiker.Naam,
                    Email = result.Gebruiker.Email,
                    IsIngelogd = true,
                    IsInterneMedewerker = result.Gebruiker.IsInterneMedewerker
                };
                AutenticatieGewijzigd?.Invoke();
                return (true, null);
            }

            return (false, result?.Foutmelding ?? "Login mislukt");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij inloggen: {ex.Message}");
            return (false, "Er is een fout opgetreden bij inloggen");
        }
    }

    // Genereer Google OAuth URL
    public string GenereerGoogleLoginUrl(string clientId, string redirectUri)
    {
        var scope = Uri.EscapeDataString("openid profile email");
        var responseType = "id_token token";
        var nonce = Guid.NewGuid().ToString();

        // Sla nonce op voor verificatie
        _ = _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "oauth_nonce", nonce);

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={clientId}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               $"response_type={responseType}&" +
               $"scope={scope}&" +
               $"nonce={nonce}";
    }

    // Verwerk OAuth callback
    public async Task<(bool Succes, string? Foutmelding)> VerwerkOAuthCallback(string idToken, string accessToken)
    {
        try
        {
            // Decode JWT token (vereenvoudigde versie - in productie zou je dit valideren)
            var payload = DecodeJwtPayload(idToken);

            if (payload.HasValue)
            {
                var payloadValue = payload.Value;
                var googleEmail = payloadValue.GetProperty("email").GetString() ?? "";
                var googleNaam = payloadValue.GetProperty("name").GetString() ?? "Onbekende gebruiker";
                var googleId = payloadValue.GetProperty("sub").GetString() ?? "";

                // Stuur Google info naar backend om gebruiker te maken/ophalen
                var externalLoginRequest = new
                {
                    Provider = "Google",
                    ProviderId = googleId,
                    Email = googleEmail,
                    Naam = googleNaam
                };

                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/external-login", externalLoginRequest);
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (result?.Succes == true && result.Gebruiker != null)
                {
                    _huidigeGebruiker = new Gebruiker
                    {
                        Id = result.Gebruiker.Id,
                        Naam = result.Gebruiker.Naam,
                        Email = result.Gebruiker.Email,
                        ProfielAfbeelding = payloadValue.TryGetProperty("picture", out var pic) ? pic.GetString() ?? "" : "",
                        IsIngelogd = true,
                        IsInterneMedewerker = result.Gebruiker.IsInterneMedewerker
                    };

                    AutenticatieGewijzigd?.Invoke();
                    return (true, null);
                }

                return (false, result?.Foutmelding ?? "Google login mislukt");
            }

            return (false, "Kon Google token niet verwerken");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij verwerken OAuth callback: {ex.Message}");
            return (false, "Er is een fout opgetreden bij Google login");
        }
    }

    private JsonElement? DecodeJwtPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var payload = parts[1];
            // Voeg padding toe indien nodig
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task Uitloggen()
    {
        try
        {
            await _httpClient.PostAsync($"{ApiBaseUrl}/logout", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij uitloggen: {ex.Message}");
        }
        finally
        {
            _huidigeGebruiker = null;
            AutenticatieGewijzigd?.Invoke();
        }
    }
}
