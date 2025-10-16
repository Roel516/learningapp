using LearningResourcesApp.Client.Services.Interfaces;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using LearningResourcesApp.Models.Auth;

namespace LearningResourcesApp.Client.Services;

public class AutenticatieService : IAutenticatieService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private Gebruiker? _huidigeGebruiker;
    private const string ApiBaseUrl = "api/account";

    public event Action? AutenticatieGewijzigd;

    public Gebruiker? HuidigeGebruiker => _huidigeGebruiker;

    public bool IsIngelogd => _huidigeGebruiker != null;

    public AutenticatieService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    // Initialiseer authenticatie - controleer of gebruiker ingelogd is
    public async Task Initialiseer()
    {
        await VoerActieMetFoutAfhandelingUit(async () =>
        {
            var response = await _httpClient.GetFromJsonAsync<AuthResponse>($"{ApiBaseUrl}/current-user");
            if (response?.Succes == true && response.Gebruiker != null)
            {
                ZetHuidigeGebruiker(response.Gebruiker);
            }
        }, "initialiseren authenticatie");
    }

    private void ZetHuidigeGebruiker(Gebruiker gebruikerInfo)
    {
        _huidigeGebruiker = new Gebruiker
        {
            Id = gebruikerInfo.Id,
            Naam = gebruikerInfo.Naam,
            Email = gebruikerInfo.Email,            
            IsInterneMedewerker = gebruikerInfo.IsInterneMedewerker
        };
        AutenticatieGewijzigd?.Invoke();
    }

    // Registreer met email/wachtwoord (Identity)
    public async Task<AuthResult> Registreren(RegisterRequest request)
    {
        return await VoerAuthenticatieActieUit(
            async () => await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/register", request),
            "Registratie mislukt",
            "registreren",
            request.IsSelfRegistration
		);
    }

    // Login met email/wachtwoord (Identity)
    public async Task<AuthResult> Inloggen(LoginRequest request)
    {
        return await VoerAuthenticatieActieUit(
            async () => await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/login", request),
            "Login mislukt",
            "inloggen",
            true
        );
    }

    private async Task<AuthResult> VoerAuthenticatieActieUit(
        Func<Task<HttpResponseMessage>> apiCall,
        string defaultFoutmelding,
        string actieBeschrijving,
        bool loggingIn)
    {
        try
        {
            var response = await apiCall();
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Succes == true && result.Gebruiker != null)
            {
                if (loggingIn)
                { 
                    ZetHuidigeGebruiker(result.Gebruiker); 
                }
                return AuthResult.Success();
            }

            return AuthResult.Failure(result?.Foutmelding ?? defaultFoutmelding);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij {actieBeschrijving}: {ex.Message}");
            return AuthResult.Failure($"Er is een fout opgetreden bij {actieBeschrijving}");
        }
    }

    private async Task<AuthResult> VoerActieMetAuthResultUit(
        Func<Task<AuthResult>> actie,
        string actieBeschrijving,
        string gebruikerFoutmelding)
    {
        try
        {
            return await actie();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij {actieBeschrijving}: {ex.Message}");
            return AuthResult.Failure($"Er is een fout opgetreden bij {gebruikerFoutmelding}");
        }
    }

    private async Task VoerActieMetFoutAfhandelingUit(Func<Task> actie, string actieBeschrijving)
    {
        try
        {
            await actie();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij {actieBeschrijving}: {ex.Message}");
        }
    }

    // Genereer Google OAuth URL
    public string GenereerGoogleLoginUrl(string clientId, string redirectUri)
    {
        var scope = Uri.EscapeDataString("openid profile email");
        var responseType = "id_token token";
        var responseMode = "fragment";
        var nonce = Guid.NewGuid().ToString();

        // Sla nonce op voor verificatie
        _ = _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "oauth_nonce", nonce);

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={clientId}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               $"response_type={responseType}&" +
               $"response_mode={responseMode}&" +
               $"scope={scope}&" +
               $"nonce={nonce}";
    }

    // Verwerk OAuth callback
    public async Task<AuthResult> VerwerkOAuthCallback(string idToken, string accessToken)
    {
        return await VoerActieMetAuthResultUit(async () =>
        {
            var gebruikerInfo = await ValideerEnDecodeToken(idToken);
            if (!gebruikerInfo.IsGeldig)
            {
                return AuthResult.Failure(gebruikerInfo.Foutmelding);
            }

            return await RegistreerExterneLogin(gebruikerInfo);
        }, "verwerken OAuth callback", "Google login");
    }

    private async Task<GebruikerInfoResult> ValideerEnDecodeToken(string idToken)
    {
        // Haal en valideer nonce
        var storedNonce = await HaalEnVerwijderNonce();
        if (string.IsNullOrEmpty(storedNonce))
        {
            return GebruikerInfoResult.Failure("Ongeldige authenticatie sessie (nonce niet gevonden)");
        }

        // Decode JWT token
        var payload = DecodeJwtPayload(idToken);
        if (!payload.HasValue)
        {
            return GebruikerInfoResult.Failure("Ongeldig ID token formaat");
        }

        var payloadValue = payload.Value;

        // Valideer JWT token
        var validatieResultaat = ValideerJwtToken(payloadValue, storedNonce);
        if (!validatieResultaat.IsGeldig)
        {
            return GebruikerInfoResult.Failure(validatieResultaat.Foutmelding);
        }

        // Extract gebruiker informatie
        return ExtractGebruikerInfo(payloadValue);
    }

    private async Task<AuthResult> RegistreerExterneLogin(GebruikerInfoResult gebruikerInfo)
    {
        // Stuur Google info naar backend om gebruiker te maken/ophalen
        var externalLoginRequest = new
        {
            Provider = "Google",
            ProviderId = gebruikerInfo.GoogleId,
            Email = gebruikerInfo.Email,
            Naam = gebruikerInfo.Naam
        };

        var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/external-login", externalLoginRequest);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (result?.Succes == true && result.Gebruiker != null)
        {
            ZetHuidigeGebruiker(result.Gebruiker);
            return AuthResult.Success();
        }

        return AuthResult.Failure(result?.Foutmelding ?? "Google login mislukt");
    }

    private async Task<string?> HaalEnVerwijderNonce()
    {
        var nonce = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "oauth_nonce");
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "oauth_nonce");
        return nonce;
    }

    private ValidationResult ValideerJwtToken(JsonElement payload, string verwachteNonce)
    {
        // Validatie 1: Controleer nonce (replay attack preventie)
        if (!payload.TryGetProperty("nonce", out var nonceElement) ||
            nonceElement.GetString() != verwachteNonce)
        {
            return ValidationResult.Failure("Ongeldige authenticatie token (nonce verificatie mislukt)");
        }

        // Validatie 2: Controleer issuer (moet Google zijn)
        if (!payload.TryGetProperty("iss", out var issElement) ||
            (issElement.GetString() != "https://accounts.google.com" &&
             issElement.GetString() != "accounts.google.com"))
        {
            return ValidationResult.Failure("Ongeldige token issuer");
        }

        // Validatie 3: Controleer expiratie
        if (payload.TryGetProperty("exp", out var expElement))
        {
            var expTime = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
            if (expTime < DateTimeOffset.UtcNow)
            {
                return ValidationResult.Failure("Token is verlopen");
            }
        }
        else
        {
            return ValidationResult.Failure("Token heeft geen expiratie timestamp");
        }

        // Validatie 4: Controleer issued-at time (niet te oud)
        if (payload.TryGetProperty("iat", out var iatElement))
        {
            var iatTime = DateTimeOffset.FromUnixTimeSeconds(iatElement.GetInt64());
            var maxAge = TimeSpan.FromMinutes(10); // Token mag niet ouder zijn dan 10 minuten
            if (DateTimeOffset.UtcNow - iatTime > maxAge)
            {
                return ValidationResult.Failure("Token is te oud");
            }
        }

        return ValidationResult.Success();
    }

    private GebruikerInfoResult ExtractGebruikerInfo(JsonElement payload)
    {
        var email = payload.GetProperty("email").GetString() ?? "";
        var naam = payload.GetProperty("name").GetString() ?? "Onbekende gebruiker";
        var googleId = payload.GetProperty("sub").GetString() ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
        {
            return GebruikerInfoResult.Failure("Onvolledige gebruikersinformatie in token");
        }

        return GebruikerInfoResult.Success(email, naam, googleId);
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
        await VoerActieMetFoutAfhandelingUit(async () =>
        {
            await _httpClient.PostAsync($"{ApiBaseUrl}/logout", null);
        }, "uitloggen");

        _huidigeGebruiker = null;
        AutenticatieGewijzigd?.Invoke();
    }
}
