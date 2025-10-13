using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using LearningResourcesApp.Client.Components.Pages.Gebruikersbeheer;
using LearningResourcesApp.Client.Models.Authenticatie;
using LearningResourcesApp.Client.Services;
using LearningResourcesApp.Client.Services.Interfaces;
using LearningResourcesApp.Client.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace LearningResourcesApp.Client.Tests;

public class GebruikersbeheerComponentTests : TestContext
{
    private FakeAutenticatieService CreateAuthService()
    {
        return new FakeAutenticatieService();
    }

    [Fact]
    public void Gebruikersbeheer_ShowsAccessDenied_WhenUserNotLoggedIn()
    {
        // Arrange
        var authService = CreateAuthService();
        authService.SetHuidigeGebruiker(null);

        Services.AddSingleton<IAutenticatieService>(_ => authService);
        Services.AddSingleton(new HttpClient());

        // Act
        var cut = RenderComponent<Gebruikersbeheer>();

        // Assert
        cut.Markup.Should().Contain("Je moet ingelogd zijn");
        cut.Markup.Should().Contain("Log hier in");
    }

    [Fact]
    public void Gebruikersbeheer_ShowsAccessDenied_WhenGoogleUserLoggedIn()
    {
        // Arrange - Google user without InterneMedewerker status
        var googleUser = new Gebruiker
        {
            Id = "google_123",
            Naam = "Google User",
            Email = "googleuser@gmail.com",
            IsIngelogd = true,
            IsInterneMedewerker = false  // Google users can't manage users
        };

        var authService = CreateAuthService();
        authService.SetHuidigeGebruiker(googleUser);

        Services.AddSingleton<IAutenticatieService>(_ => authService);
        Services.AddSingleton(new HttpClient());

        // Act
        var cut = RenderComponent<Gebruikersbeheer>();

        // Assert
        cut.Markup.Should().Contain("Je hebt geen toegang tot deze pagina");
        cut.Markup.Should().Contain("Alleen interne medewerkers kunnen gebruikers beheren");
        cut.Markup.Should().NotContain("Nieuwe Gebruiker Toevoegen");
    }

    [Fact]
    public void Gebruikersbeheer_ShowsUserManagementForm_WhenInterneMedewerkerLoggedIn()
    {
        // Arrange - Internal employee
        var interneMedewerker = new Gebruiker
        {
            Id = "admin1",
            Naam = "Admin User",
            Email = "admin@company.com",
            IsIngelogd = true,
            IsInterneMedewerker = true
        };

        var authService = CreateAuthService();
        authService.SetHuidigeGebruiker(interneMedewerker);

        Services.AddSingleton<IAutenticatieService>(_ => authService);
        Services.AddSingleton(new HttpClient());

        // Act
        var cut = RenderComponent<Gebruikersbeheer>();

        // Assert
        cut.Markup.Should().Contain("Gebruikersbeheer");
        cut.Markup.Should().Contain("Nieuwe Gebruiker Toevoegen");
        cut.Markup.Should().Contain("Bestaande Gebruikers");
        cut.Markup.Should().NotContain("Je hebt geen toegang");
    }

    [Fact]
    public void Gebruikersbeheer_ShowsAddUserForm_WhenInterneMedewerkerLoggedIn()
    {
        // Arrange
        var interneMedewerker = new Gebruiker
        {
            Id = "admin1",
            Naam = "Admin",
            Email = "admin@company.com",
            IsIngelogd = true,
            IsInterneMedewerker = true
        };

        var authService = CreateAuthService();
        authService.SetHuidigeGebruiker(interneMedewerker);

        Services.AddSingleton<IAutenticatieService>(_ => authService);
        Services.AddSingleton(new HttpClient());

        // Act
        var cut = RenderComponent<Gebruikersbeheer>();

        // Assert - Should have form fields
        cut.Markup.Should().Contain("Naam");
        cut.Markup.Should().Contain("Email");
        cut.Markup.Should().Contain("Wachtwoord");
        cut.Markup.Should().Contain("Gebruiker Toevoegen");
    }

    [Fact]
    public void Gebruikersbeheer_HasCorrectFormInputs()
    {
        // Arrange
        var interneMedewerker = new Gebruiker
        {
            Id = "admin1",
            Naam = "Admin",
            Email = "admin@company.com",
            IsIngelogd = true,
            IsInterneMedewerker = true
        };

        var authService = CreateAuthService();
        authService.SetHuidigeGebruiker(interneMedewerker);

        Services.AddSingleton<IAutenticatieService>(_ => authService);
        Services.AddSingleton(new HttpClient());

        // Act
        var cut = RenderComponent<Gebruikersbeheer>();

        // Assert - Check for input fields
        var naamInput = cut.Find("input#naam");
        var emailInput = cut.Find("input#email");
        var wachtwoordInput = cut.Find("input#wachtwoord");

        naamInput.Should().NotBeNull();
        emailInput.Should().NotBeNull();
        emailInput.GetAttribute("type").Should().Be("email");
        wachtwoordInput.Should().NotBeNull();
        wachtwoordInput.GetAttribute("type").Should().Be("password");
    }

    [Fact]
    public void Gebruikersbeheer_ShowsWarningMessage_ForPasswordRequirements()
    {
        // Arrange
        var interneMedewerker = new Gebruiker
        {
            Id = "admin1",
            Naam = "Admin",
            Email = "admin@company.com",
            IsIngelogd = true,
            IsInterneMedewerker = true
        };

        var authService = CreateAuthService();
        authService.SetHuidigeGebruiker(interneMedewerker);

        Services.AddSingleton<IAutenticatieService>(_ => authService);
        Services.AddSingleton(new HttpClient());

        // Act
        var cut = RenderComponent<Gebruikersbeheer>();

        // Assert
        cut.Markup.Should().Contain("Minimaal 6 karakters");
    }    
}
