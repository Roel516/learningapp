# Oplossing voor "redirect_uri_mismatch" Fout

## Het Probleem
Je krijgt de fout `400: redirect_uri_mismatch` omdat de redirect URI die de app gebruikt niet exact overeenkomt met wat is geconfigureerd in Google Cloud Console.

## De Oplossing

### Stap 1: Ga naar Google Cloud Console
1. Ga naar [Google Cloud Console](https://console.cloud.google.com/)
2. Selecteer je project
3. Ga naar **APIs & Services** → **Credentials**
4. Klik op je OAuth 2.0 Client ID (degene die eindigt met `.apps.googleusercontent.com`)

### Stap 2: Voeg je Redirect URI toe met de juiste poort

⚠️ **BELANGRIJKE UPDATE:** De app gebruikt nu **automatisch de juiste poort**!

**Controleer eerst welke poort je app gebruikt:**
1. Start de app met `dotnet run`
2. Kijk in de console output naar de regel die zegt: `Now listening on: http://localhost:XXXX`
3. Noteer het poortnummer (bijvoorbeeld 5287)

**Voeg dan deze URI toe in Google Cloud Console:**

Als je app draait op poort 5287:
```
http://localhost:5287/authentication/login-callback
```

Als je app draait op poort 5000:
```
http://localhost:5000/authentication/login-callback
```

**TIP:** Je kunt meerdere redirect URIs toevoegen voor verschillende poorten als je wilt:
```
http://localhost:5000/authentication/login-callback
http://localhost:5287/authentication/login-callback
http://localhost:7000/authentication/login-callback
```

**Belangrijk:**
- Zorg dat er **GEEN trailing slash** is aan het einde
- Het pad moet exact `/authentication/login-callback` zijn
- Gebruik het **exacte poortnummer** dat in de console wordt getoond

### Stap 3: Voeg ook JavaScript Origins toe

In het veld **Authorized JavaScript origins**, voeg het poortnummer van je app toe:

Als je app draait op poort 5287:
```
http://localhost:5287
```

Je kunt ook meerdere poorten toevoegen:
```
http://localhost:5000
http://localhost:5287
http://localhost:7000
```

**Let op:** Bij JavaScript Origins gebruik je **ALLEEN** de base URL (zonder pad)!

### Stap 4: Klik op Save

Wacht ongeveer 5-10 seconden voordat je opnieuw probeert in te loggen.

## Veelvoorkomende Fouten

❌ **FOUT:** `http://localhost:5000/authentication/login-callback/` (met trailing slash)
✅ **GOED:** `http://localhost:5000/authentication/login-callback` (zonder trailing slash)

❌ **FOUT:** Alleen HTTPS toevoegen
✅ **GOED:** Beide HTTP en HTTPS toevoegen

❌ **FOUT:** Verkeerde poort (bijv. 5002 in plaats van 5001)
✅ **GOED:** Exact de poort die de app gebruikt (5000 voor HTTP, 5001 voor HTTPS)

## Controleer welke URL je app gebruikt

Als je niet zeker weet welke poort je app gebruikt:

1. Start de app met `dotnet run`
2. Kijk naar de console output, je ziet iets als:
   ```
   Now listening on: http://localhost:5000
   Now listening on: https://localhost:5001
   ```
3. Gebruik de poort die wordt getoond

## Test de configuratie

Na het opslaan:
1. Herstart je browser (of gebruik incognito mode)
2. Ga naar de inlogpagina
3. Klik op "Inloggen met Google OAuth"
4. Je zou nu moeten worden doorgestuurd naar Google's login pagina

## Als het nog steeds niet werkt

1. **Wacht 5-10 minuten** - Google kan even nodig hebben om de wijzigingen door te voeren
2. **Clear je browser cache** - Oude OAuth redirects kunnen gecached zijn
3. **Controleer de exacte error** - Kijk in de browser console (F12) voor meer details
4. **Gebruik incognito mode** - Dit voorkomt cache problemen
5. **Controleer de URL in de foutmelding** - Google toont welke redirect_uri werd verwacht vs wat werd ontvangen

## Debug Tip

Open de browser console (F12) en kijk naar de Network tab. Je zult de redirect naar Google zien met de volledige URL inclusief alle parameters. Kopieer de `redirect_uri` parameter en controleer of deze EXACT overeenkomt met wat in Google Cloud Console staat.
