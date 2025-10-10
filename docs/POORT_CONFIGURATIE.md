# Poort Configuratie

## ✅ Poorten zijn nu vastgezet!

De applicatie draait nu altijd op deze poorten:
- **HTTP:** `http://localhost:5000`
- **HTTPS:** `https://localhost:5001`

## Wat je NU moet doen in Google Cloud Console:

### Stap 1: Ga naar Google Cloud Console
1. Ga naar [Google Cloud Console](https://console.cloud.google.com/)
2. Selecteer je project
3. Ga naar **APIs & Services** → **Credentials**
4. Klik op je OAuth 2.0 Client ID

### Stap 2: Voeg deze Redirect URIs toe

In het veld **"Authorized redirect URIs"**, voeg toe:

```
http://localhost:5000/authentication/login-callback
https://localhost:5001/authentication/login-callback
```

### Stap 3: Voeg deze JavaScript Origins toe

In het veld **"Authorized JavaScript origins"**, voeg toe:

```
http://localhost:5000
https://localhost:5001
```

### Stap 4: Klik op SAVE

Wacht 5-10 seconden voordat je opnieuw probeert in te loggen.

## De app starten

Start de app met een van deze commando's:

### Voor HTTP (poort 5000):
```bash
dotnet run --launch-profile http
```

### Voor HTTPS (poort 5001):
```bash
dotnet run --launch-profile https
```

Of gewoon:
```bash
dotnet run
```
(gebruikt standaard het https profiel)

## Testen

1. Start de app
2. Ga naar de inlogpagina
3. Klik op "Inloggen met Google OAuth"
4. Je zou nu geen redirect_uri_mismatch fout meer moeten krijgen!

## Troubleshooting

Als je nog steeds een fout krijgt:
1. Controleer of de URIs in Google Cloud Console **exact** overeenkomen (geen extra spaties of trailing slashes)
2. Wacht 5-10 minuten (Google kan even nodig hebben om de wijzigingen door te voeren)
3. Clear je browser cache of gebruik incognito mode
4. Check de browser console (F12) voor meer details over de fout
