# Google OAuth 2.0 Configuratie Handleiding

Deze handleiding legt uit hoe je Google OAuth 2.0 kunt configureren voor de Leermiddelen App.

## Stap 1: Google Cloud Project aanmaken

1. Ga naar [Google Cloud Console](https://console.cloud.google.com/)
2. Klik op "Select a project" → "New Project"
3. Geef je project een naam (bijv. "Leermiddelen App")
4. Klik op "Create"

## Stap 2: OAuth Consent Screen configureren

1. In het linker menu, ga naar **APIs & Services** → **OAuth consent screen**
2. Selecteer **External** als user type en klik op **Create**
3. Vul de vereiste informatie in:
   - **App name**: Leermiddelen App
   - **User support email**: Jouw email adres
   - **Developer contact information**: Jouw email adres
4. Klik op **Save and Continue**
5. Bij **Scopes**: Klik op **Add or Remove Scopes**
   - Selecteer de volgende scopes:
     - `openid`
     - `profile`
     - `email`
6. Klik op **Save and Continue**
7. Bij **Test users**: Voeg je eigen email adres toe als test gebruiker
8. Klik op **Save and Continue**

## Stap 3: OAuth 2.0 Client ID aanmaken

1. Ga naar **APIs & Services** → **Credentials**
2. Klik op **+ CREATE CREDENTIALS** → **OAuth client ID**
3. Selecteer **Web application** als application type
4. Geef een naam op (bijv. "Leermiddelen App Client")
5. Voeg **Authorized JavaScript origins** toe:
   - `http://localhost:5000`
   - `https://localhost:5001`
   - (Voeg je productie URL toe als je die hebt)
6. Voeg **Authorized redirect URIs** toe:
   - `http://localhost:5000/authentication/login-callback`
   - `https://localhost:5001/authentication/login-callback`
   - (Voeg je productie callback URL toe als je die hebt)
7. Klik op **Create**
8. **Kopieer de Client ID** die wordt getoond

## Stap 4: Client ID toevoegen aan de applicatie

1. Open `wwwroot/appsettings.json` in je project
2. Vervang `YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com` met je echte Client ID:

```json
{
  "Google": {
    "ClientId": "123456789-abc123xyz.apps.googleusercontent.com",
    "Authority": "https://accounts.google.com",
    "RedirectUri": "https://localhost:5001/authentication/login-callback",
    "PostLogoutRedirectUri": "https://localhost:5001/"
  }
}
```

**Let op**:
- Je hoeft **geen Client Secret** te configureren voor een Blazor WebAssembly applicatie (client-side only)
- De Client ID mag publiek zichtbaar zijn in de browser

## Stap 5: Applicatie testen

1. Start de applicatie met `dotnet run`
2. Navigeer naar de inlogpagina
3. Klik op "Inloggen met Google OAuth"
4. Je wordt doorgestuurd naar Google's login scherm
5. Log in met je Google account
6. Geef toestemming aan de app om je profielgegevens te gebruiken
7. Je wordt teruggestuurd naar de applicatie en bent ingelogd

## Productie Deployment

Voor een productie omgeving:

1. Voeg je productie URL toe aan de **Authorized JavaScript origins** en **Authorized redirect URIs** in Google Cloud Console
2. Update `appsettings.json` met je productie URL's
3. Zorg dat je OAuth consent screen is geverifieerd door Google (voor productie gebruik)
4. Overweeg om de app status te wijzigen van "Testing" naar "In production" in de OAuth consent screen

## Security Best Practices

- ✅ Gebruik HTTPS in productie (altijd!)
- ✅ Valideer de `nonce` in de callback (wordt automatisch gedaan)
- ✅ Controleer de `aud` (audience) claim in het ID token
- ✅ Stel een maximum token levensduur in
- ✅ Gebruik state parameter voor CSRF bescherming (optioneel)

## Veelvoorkomende Problemen

### "redirect_uri_mismatch" error
- Controleer of de redirect URI in Google Cloud Console exact overeenkomt met de URL in je applicatie (inclusief protocol, poort, en pad)

### "access_denied" error
- Zorg dat je email adres is toegevoegd als test gebruiker in de OAuth consent screen
- Controleer of de scopes correct zijn geconfigureerd

### Token validatie faalt
- Zorg dat je systeem klok correct is ingesteld
- Controleer of de Client ID correct is geconfigureerd

## Demo Mode

Als je Google OAuth nog niet hebt geconfigureerd, kun je de "Demo Login" knop gebruiken om de applicatie te testen zonder echte Google credentials.

## Meer informatie

- [Google OAuth 2.0 Documentatie](https://developers.google.com/identity/protocols/oauth2)
- [Google Identity Platform](https://developers.google.com/identity)
