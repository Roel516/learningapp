# Leermiddelen App

Een Blazor WebAssembly applicatie voor het beheren en organiseren van leermiddelen met gebruikersreacties.

## Functionaliteiten

- **Leermiddelen Toevoegen**: Upload leermiddelen met titel, beschrijving en link
- **Alle Leermiddelen Bekijken**: Blader door al je leermateriaal in een kaart-gebaseerde layout
- **Leermiddel Details**: Bekijk gedetailleerde informatie over elk leermiddel
- **Reactiesysteem**: Voeg reacties toe en bekijk reacties op elk leermiddel
- **Responsive Design**: Gebouwd met Bootstrap voor mobiel-vriendelijke ervaring

## Projectstructuur

```
LearningResourcesApp/
├── Models/
│   ├── LearningResource.cs    # Hoofd leermiddel model
│   └── Comment.cs              # Reactie model
├── Services/
│   └── LearningResourceService.cs  # Service voor het beheren van leermiddelen
├── Components/Pages/
│   ├── AddResource.razor       # Formulier om nieuwe leermiddelen toe te voegen
│   ├── ResourceList.razor      # Lijstweergave van alle leermiddelen
│   └── ResourceDetails.razor   # Detailweergave met reacties
├── Pages/
│   └── Home.razor              # Landingspagina
└── Layout/
    └── NavMenu.razor           # Navigatiemenu
```

## De Applicatie Uitvoeren

1. Zorg ervoor dat je .NET 9.0 SDK geïnstalleerd hebt
2. Navigeer naar de projectdirectory
3. Start de applicatie:
   ```bash
   dotnet run
   ```
4. Open je browser en navigeer naar de URL die in de console wordt weergegeven (meestal https://localhost:5001)

## Gebruik

1. **Homepagina**: Klik op "Bekijk Alle Leermiddelen" of "Nieuw Leermiddel Toevoegen" om te beginnen
2. **Leermiddel Toevoegen**: Vul de titel, beschrijving en link in voor je leermiddel
3. **Leermiddelen Bekijken**: Blader door alle leermiddelen in een kaart layout
4. **Details Bekijken**: Klik op "Bekijk Details" om volledige informatie en reacties te zien
5. **Reacties Toevoegen**: Voeg op de detailpagina je naam en reactie toe om het leermiddel te bespreken

## Technologie Stack

- Blazor WebAssembly (.NET 9.0)
- Bootstrap 5 voor styling
- C# voor business logica
- In-memory data opslag (staat blijft behouden tijdens sessie)

## Opmerkingen

- Data wordt in-memory opgeslagen en gaat verloren wanneer de applicatie wordt afgesloten
- Om data te behouden, zou je een backend API met een database moeten toevoegen
