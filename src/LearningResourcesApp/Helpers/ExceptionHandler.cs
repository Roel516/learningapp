namespace LearningResourcesApp.Helpers;

public static class ExceptionHandler
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        ILogger logger,
        string errorMessage,
        params object[] logParams)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, errorMessage, logParams);
            throw;
        }
    }

    public static async Task ExecuteAsync(
        Func<Task> operation,
        ILogger logger,
        string errorMessage,
        params object[] logParams)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, errorMessage, logParams);
            throw;
        }
    }
}
