# Leermiddelen App

Een Blazor WebAssembly applicatie voor het beheren en organiseren van leermiddelen met gebruikersreacties.

## Functionaliteiten

- **Leermiddelen Toevoegen**: Upload leermiddelen met titel, beschrijving en link
- **Alle Leermiddelen Bekijken**: Blader door al je leermateriaal in een kaart-gebaseerde layout
- **Leermiddel Details**: Bekijk gedetailleerde informatie over elk leermiddel
- **Reactiesysteem**: Voeg reacties toe en bekijk reacties op elk leermiddel
- **Responsive Design**: Gebouwd met Bootstrap voor mobiel-vriendelijke ervaring

## De Applicatie Uitvoeren

1. Zorg ervoor dat je .NET 8.0 SDK geïnstalleerd hebt
2. Navigeer naar de projectdirectory
3. Start de applicatie:
   ```bash
   dotnet run --project src/LearningResourcesApp/LearningResourcesApp.csproj
   ```
4. Open je browser en navigeer naar de URL die in de console wordt weergegeven (meestal https://localhost:5001)

## Gebruik

1. **Homepagina**: Klik op "Bekijk Alle Leermiddelen" of "Nieuw Leermiddel Toevoegen" om te beginnen
2. **Leermiddel Toevoegen**: Vul de titel, beschrijving en link in voor je leermiddel
3. **Leermiddelen Bekijken**: Blader door alle leermiddelen in een kaart layout
4. **Details Bekijken**: Klik op "Bekijk Details" om volledige informatie en reacties te zien
5. **Reacties Toevoegen**: Voeg op de detailpagina je naam en reactie toe om het leermiddel te bespreken

## Technologie Stack

- Blazor WebAssembly (.NET 8.0)
- Bootstrap 5 voor styling
- C# voor business logica
- In-memory data opslag (staat blijft behouden tijdens sessie)

