using Bunit;
using FluentAssertions;
using LearningResourcesApp.Client.Layout;
using LearningResourcesApp.Models.Auth;
using LearningResourcesApp.Client.Services.Interfaces;
using LearningResourcesApp.Client.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace LearningResourcesApp.Client.Tests;

public class NavMenuComponentTests : TestContext
{
	private FakeAutenticatieService CreateAuthService()
	{
		return new FakeAutenticatieService();
	}

	[Fact]
	public void NavMenu_ShowsLoginAndRegisterLinks_WhenUserNotLoggedIn()
	{
		// Arrange
		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(null);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Inloggen");
		cut.Markup.Should().Contain("Registreren");
		cut.Markup.Should().NotContain("Gebruikersbeheer");
	}

	[Fact]
	public void NavMenu_ShowsUserName_WhenUserLoggedIn()
	{
		// Arrange
		var gebruiker = new Gebruiker
		{
			Id = "user1",
			Naam = "Test User",
			Email = "test@example.com",
			IsInterneMedewerker = false
		};

		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(gebruiker);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Test User");
		cut.Markup.Should().Contain("Uitloggen");
	}

	[Fact]
	public void NavMenu_HidesGebruikersbeheerLink_WhenNotInterneMedewerkerLoggedIn()
	{
		// Arrange - Google user without InterneMedewerker status
		var googleUser = new Gebruiker
		{
			Id = "google_user_123",
			Naam = "Google User",
			Email = "googleuser@gmail.com",
			IsInterneMedewerker = false
		};

		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(googleUser);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		cut.Markup.Should().NotContain("Gebruikersbeheer");
		cut.Markup.Should().NotContain("gebruikersbeheer");
		cut.Markup.Should().NotContain("Leermiddel Toevoegen");
		cut.Markup.Should().NotContain("Reacties Beoordelen");

		// Should show user name and logout
		cut.Markup.Should().Contain("Google User");
		cut.Markup.Should().Contain("Uitloggen");

		// Should still show home link
		cut.Markup.Should().Contain("Leermiddelen");
	}

	[Fact]
	public void NavMenu_ShowsGebruikersbeheerLink_WhenInterneMedewerkerLoggedIn()
	{
		// Arrange - Internal employee
		var interneMedewerker = new Gebruiker
		{
			Id = "admin1",
			Naam = "Admin User",
			Email = "admin@company.com",
			IsInterneMedewerker = true
		};

		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(interneMedewerker);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Gebruikersbeheer");
		cut.Markup.Should().Contain("Leermiddel Toevoegen");
		cut.Markup.Should().Contain("Reacties Beoordelen");
		cut.Markup.Should().Contain("Admin User");
	}

	[Fact]
	public void NavMenu_ShowsAllInterneMedewerkerLinks_OnlyForInternalEmployees()
	{
		// Arrange
		var regularUser = new Gebruiker
		{
			Id = "user1",
			Naam = "Regular User",
			Email = "user@example.com",
			IsInterneMedewerker = false
		};

		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(regularUser);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert - Should NOT have any admin links
		var addResourceLinks = cut.FindAll("a[href='add-resource']");
		var reviewLinks = cut.FindAll("a[href='reactie-review']");
		var userManagementLinks = cut.FindAll("a[href='gebruikersbeheer']");

		addResourceLinks.Should().BeEmpty();
		reviewLinks.Should().BeEmpty();
		userManagementLinks.Should().BeEmpty();
	}

	[Fact]
	public void NavMenu_AlwaysShowsLeermiddelenLink()
	{
		// Arrange - Anonymous user
		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(null);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Leermiddelen");
		var homeLinks = cut.FindAll("a[href='']");
		homeLinks.Should().NotBeEmpty();
	}

	[Fact]
	public void NavMenu_ShowsCorrectIconsForInterneMedewerker()
	{
		// Arrange
		var interneMedewerker = new Gebruiker
		{
			Id = "admin1",
			Naam = "Admin",
			Email = "admin@company.com",
			IsInterneMedewerker = true
		};

		var authService = CreateAuthService();
		authService.SetHuidigeGebruiker(interneMedewerker);

		Services.AddSingleton<IAutenticatieService>(_ => authService);
		Services.AddSingleton(new HttpClient());

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert - Check for Bootstrap icons
		cut.Markup.Should().Contain("bi-plus-square-fill-nav-menu"); // Add resource icon
		cut.Markup.Should().Contain("bi-clipboard-check"); // Review icon
		cut.Markup.Should().Contain("bi-people-fill"); // User management icon
	}
}
