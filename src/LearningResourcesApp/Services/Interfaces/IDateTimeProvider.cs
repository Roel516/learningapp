namespace LearningResourcesApp.Services.Interfaces;

/// <summary>
/// Provides the current date and time. This abstraction allows for easier testing and consistency across time zones.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }
}
