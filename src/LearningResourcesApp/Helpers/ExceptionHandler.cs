namespace LearningResourcesApp.Helpers;

public class ExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string errorMessage,
        params object[] logParams)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, logParams);
            throw;
        }
    }

    public async Task ExecuteAsync(
        Func<Task> operation,
        string errorMessage,
        params object[] logParams)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, logParams);
            throw;
        }
    }
}
