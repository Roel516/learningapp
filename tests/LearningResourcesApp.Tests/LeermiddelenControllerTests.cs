using FluentAssertions;
using LearningResourcesApp.Controllers;
using LearningResourcesApp.Models;
using LearningResourcesApp.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace LearningResourcesApp.Tests;

public class LeermiddelenControllerTests
{
    private readonly Mock<ILeermiddelRepository> _mockLeermiddelRepo;
    private readonly Mock<IReactieRepository> _mockReactieRepo;
    private readonly Mock<Helpers.ControllerExceptionHandler> _mockExceptionHandler;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly LeermiddelenController _controller;

    public LeermiddelenControllerTests()
    {
        // Setup repository mocks
        _mockLeermiddelRepo = new Mock<ILeermiddelRepository>();
        _mockReactieRepo = new Mock<IReactieRepository>();

        // Setup exception handler mock
        var loggerMock = new Mock<ILogger<Helpers.ControllerExceptionHandler>>();
        _mockExceptionHandler = new Mock<Helpers.ControllerExceptionHandler>(loggerMock.Object);

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _controller = new LeermiddelenController(
            _mockLeermiddelRepo.Object,
            _mockReactieRepo.Object,
            _mockExceptionHandler.Object,
            _mockUserManager.Object);
    }

    [Fact]
    public async Task GetLeermiddelen_ReturnsAllLeermiddelen()
    {
        // Arrange
        var leermiddelen = new List<Leermiddel>
        {
            new Leermiddel
            {
                Id = Guid.NewGuid(),
                Titel = "Test 1",
                Beschrijving = "Beschrijving 1",
                Link = "https://test1.com",
                AangemaaktOp = DateTime.UtcNow,
                Reacties = new List<Reactie>()
            },
            new Leermiddel
            {
                Id = Guid.NewGuid(),
                Titel = "Test 2",
                Beschrijving = "Beschrijving 2",
                Link = "https://test2.com",
                AangemaaktOp = DateTime.UtcNow,
                Reacties = new List<Reactie>()
            }
        };

        _mockLeermiddelRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(leermiddelen);

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
        var returnedLeermiddelen = okResult?.Value as List<Leermiddel>;
        returnedLeermiddelen.Should().NotBeNull();
        returnedLeermiddelen!.Count.Should().Be(2);
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

        _mockLeermiddelRepo.Setup(x => x.GetByIdAsync(leermiddel.Id))
            .ReturnsAsync(leermiddel);

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

        _mockLeermiddelRepo.Setup(x => x.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Leermiddel?)null);

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
    public async Task CreateLeermiddel_WithValidData_CreatesLeermiddel()
    {
        // Arrange
        var leermiddel = new Leermiddel
        {
            Titel = "New Test",
            Beschrijving = "New Beschrijving",
            Link = "https://newtest.com"
        };

        var createdLeermiddel = new Leermiddel
        {
            Id = Guid.NewGuid(),
            Titel = leermiddel.Titel,
            Beschrijving = leermiddel.Beschrijving,
            Link = leermiddel.Link,
            AangemaaktOp = DateTime.UtcNow,
            Reacties = new List<Reactie>()
        };

        _mockLeermiddelRepo.Setup(x => x.CreateAsync(It.IsAny<Leermiddel>()))
            .ReturnsAsync(createdLeermiddel);

        // Act
        var result = await _controller.CreateLeermiddel(leermiddel);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var returnedLeermiddel = createdResult?.Value as Leermiddel;
        returnedLeermiddel.Should().NotBeNull();
        returnedLeermiddel!.Titel.Should().Be(leermiddel.Titel);
        returnedLeermiddel.Id.Should().NotBe(Guid.Empty);
    }
}
