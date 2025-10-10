using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace LearningResourcesApp.Client.Tests;

public class SimpleComponentTests : TestContext
{
    [Fact]
    public void Example_Test_AlwaysPasses()
    {
        // This is a placeholder test to demonstrate the bUnit test setup
        // In a real project, you would add actual component tests here
        var result = 1 + 1;
        result.Should().Be(2);
    }

    [Fact]
    public void TestContext_CanBeInitialized()
    {
        // Arrange & Act
        var services = Services;

        // Assert
        services.Should().NotBeNull();
    }

    [Fact]
    public void Services_CanBeRegistered()
    {
        // Arrange
        Services.AddScoped<TestService>();

        // Act
        var service = Services.BuildServiceProvider().GetService<TestService>();

        // Assert
        service.Should().NotBeNull();
    }

    private class TestService
    {
        public string GetMessage() => "Test";
    }
}
