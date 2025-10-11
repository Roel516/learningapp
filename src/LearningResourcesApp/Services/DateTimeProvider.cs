using LearningResourcesApp.Services.Interfaces;

namespace LearningResourcesApp.Services;

/// <summary>
/// Default implementation of IDateTimeProvider that returns the actual system time.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
