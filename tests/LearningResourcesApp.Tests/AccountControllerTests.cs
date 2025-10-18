using FluentAssertions;
using LearningResourcesApp.Controllers;
using LearningResourcesApp.Models;
using LearningResourcesApp.Models.Auth;
using LearningResourcesApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace LearningResourcesApp.Tests;

public class AccountControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly Mock<SignInManager<IdentityUser>> _mockSignInManager;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Setup SignInManager mock
        var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        _mockSignInManager = new Mock<SignInManager<IdentityUser>>(
            _mockUserManager.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null, null, null, null);

        // Setup JwtTokenService mock
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockJwtTokenService.Setup(x => x.GenerateTokenAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync("mock-jwt-token");

        _controller = new AccountController(_mockUserManager.Object, _mockSignInManager.Object, _mockJwtTokenService.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Naam = "Test User",
            Email = "test@example.com",
            Wachtwoord = "Test123"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((IdentityUser?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), request.Wachtwoord))
            .ReturnsAsync(IdentityResult.Success);

        _mockSignInManager.Setup(x => x.SignInAsync(It.IsAny<IdentityUser>(), true, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.Succes.Should().BeTrue();
        response.Gebruiker.Should().NotBeNull();
        response.Gebruiker!.Email.Should().Be(request.Email);
        response.Gebruiker.Naam.Should().Be(request.Naam);
        response.Gebruiker.IsInterneMedewerker.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Naam = "Test User",
            Email = "existing@example.com",
            Wachtwoord = "Test123"
        };

        var existingUser = new IdentityUser
		{
            Email = request.Email,
            UserName = request.Email
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        var response = badRequestResult?.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.Succes.Should().BeFalse();
        response.Foutmelding.Should().Contain("bestaat al");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Wachtwoord = "Test123"
        };

        var user = new IdentityUser
		{
            Id = "user123",
            Email = request.Email,
            UserName = "Test User",
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Wachtwoord))
            .ReturnsAsync(true);

        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim>());

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.Succes.Should().BeTrue();
        response.Gebruiker.Should().NotBeNull();
        response.Gebruiker!.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Wachtwoord = "Test123"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        var response = unauthorizedResult?.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.Succes.Should().BeFalse();
        response.Foutmelding.Should().Contain("Ongeldige email");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Wachtwoord = "WrongPassword"
        };

        var user = new IdentityUser
		{
            Email = request.Email,
            UserName = "Test User"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Wachtwoord))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        var response = unauthorizedResult?.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.Succes.Should().BeFalse();
        response.Foutmelding.Should().Contain("Ongeldige email");
    }

    [Fact]
    public async Task Login_WithInterneMedewerkerClaim_ReturnsUserWithInterneMedewerkerTrue()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "admin@example.com",
            Wachtwoord = "Admin123"
        };

        var user = new IdentityUser
		{
            Id = "admin123",
            Email = request.Email,
            UserName = "Admin User",
        };

        var claims = new List<Claim>
        {
            new Claim(AppClaims.InterneMedewerker, "true")
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Wachtwoord))
            .ReturnsAsync(true);

        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(claims);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as AuthResponse;
        response.Should().NotBeNull();
        response!.Succes.Should().BeTrue();
        response.Gebruiker.Should().NotBeNull();
        response.Gebruiker!.IsInterneMedewerker.Should().BeTrue();
    }
}
