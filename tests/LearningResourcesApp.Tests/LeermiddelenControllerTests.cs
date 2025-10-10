using FluentAssertions;
using LearningResourcesApp.Controllers;
using LearningResourcesApp.Data;
using LearningResourcesApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace LearningResourcesApp.Tests;

public class LeermiddelenControllerTests : IDisposable
{
    private readonly LeermiddelContext _context;
    private readonly Mock<ILogger<LeermiddelenController>> _mockLogger;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly LeermiddelenController _controller;

    public LeermiddelenControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LeermiddelContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new LeermiddelContext(options);

        // Setup logger mock
        _mockLogger = new Mock<ILogger<LeermiddelenController>>();

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _controller = new LeermiddelenController(_context, _mockLogger.Object, _mockUserManager.Object);
    }

    [Fact]
    public async Task GetLeermiddelen_ReturnsAllLeermiddelen()
    {
        // Arrange
        var leermiddel1 = new Leermiddel
        {
            Id = Guid.NewGuid(),
            Titel = "Test 1",
            Beschrijving = "Beschrijving 1",
            Link = "https://test1.com",
            AangemaaktOp = DateTime.UtcNow,
            Reacties = new List<Reactie>()
        };
        var leermiddel2 = new Leermiddel
        {
            Id = Guid.NewGuid(),
            Titel = "Test 2",
            Beschrijving = "Beschrijving 2",
            Link = "https://test2.com",
            AangemaaktOp = DateTime.UtcNow,
            Reacties = new List<Reactie>()
        };

        _context.Leermiddelen.AddRange(leermiddel1, leermiddel2);
        await _context.SaveChangesAsync();

        // Setup anonymous user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetLeermiddelen();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var leermiddelen = okResult?.Value as List<Leermiddel>;
        leermiddelen.Should().NotBeNull();
        leermiddelen!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetLeermiddel_WithValidId_ReturnsLeermiddel()
    {
        // Arrange
        var leermiddel = new Leermiddel
        {
            Id = Guid.NewGuid(),
            Titel = "Test",
            Beschrijving = "Beschrijving",
            Link = "https://test.com",
            AangemaaktOp = DateTime.UtcNow,
            Reacties = new List<Reactie>()
        };

        _context.Leermiddelen.Add(leermiddel);
        await _context.SaveChangesAsync();

        // Setup anonymous user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetLeermiddel(leermiddel.Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedLeermiddel = okResult?.Value as Leermiddel;
        returnedLeermiddel.Should().NotBeNull();
        returnedLeermiddel!.Id.Should().Be(leermiddel.Id);
        returnedLeermiddel.Titel.Should().Be(leermiddel.Titel);
    }

    [Fact]
    public async Task GetLeermiddel_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Setup anonymous user
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetLeermiddel(nonExistentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateLeermiddel_WithoutInterneMedewerkerClaim_ReturnsForbid()
    {
        // Arrange
        var leermiddel = new Leermiddel
        {
            Titel = "New Test",
            Beschrijving = "New Beschrijving",
            Link = "https://newtest.com"
        };

        var user = new ApplicationUser { Id = "user1" };
        var claims = new List<Claim>();

        // Setup authenticated user without InterneMedewerker claim
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(claims);

        // Act
        var result = await _controller.CreateLeermiddel(leermiddel);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateLeermiddel_WithInterneMedewerkerClaim_CreatesLeermiddel()
    {
        // Arrange
        var leermiddel = new Leermiddel
        {
            Titel = "New Test",
            Beschrijving = "New Beschrijving",
            Link = "https://newtest.com"
        };

        var user = new ApplicationUser { Id = "admin1" };
        var claims = new List<Claim>
        {
            new Claim(AppClaims.InterneMedewerker, "true")
        };

        // Setup authenticated admin user
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(claims);

        // Act
        var result = await _controller.CreateLeermiddel(leermiddel);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var createdLeermiddel = createdResult?.Value as Leermiddel;
        createdLeermiddel.Should().NotBeNull();
        createdLeermiddel!.Titel.Should().Be(leermiddel.Titel);
        createdLeermiddel.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task DeleteLeermiddel_WithoutInterneMedewerkerClaim_ReturnsForbid()
    {
        // Arrange
        var leermiddel = new Leermiddel
        {
            Id = Guid.NewGuid(),
            Titel = "Test",
            Beschrijving = "Beschrijving",
            Link = "https://test.com",
            AangemaaktOp = DateTime.UtcNow
        };

        _context.Leermiddelen.Add(leermiddel);
        await _context.SaveChangesAsync();

        var user = new ApplicationUser { Id = "user1" };
        var claims = new List<Claim>();

        // Setup authenticated user without InterneMedewerker claim
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(claims);

        // Act
        var result = await _controller.DeleteLeermiddel(leermiddel.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
