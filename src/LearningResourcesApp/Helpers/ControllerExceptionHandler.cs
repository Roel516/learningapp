using Microsoft.AspNetCore.Mvc;

namespace LearningResourcesApp.Helpers;

public class ControllerExceptionHandler
{
    private readonly ILogger<ControllerExceptionHandler> _logger;

    public ControllerExceptionHandler(ILogger<ControllerExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> ExecuteAsync(
        Func<Task<IActionResult>> operation,
        string errorMessage,
        string userFriendlyMessage,
        params object[] logParams)
    {
        try
        {
            return await operation();
        }
        catch (KeyNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, logParams);
            return new ObjectResult(userFriendlyMessage) { StatusCode = 500 };
        }
    }

    public async Task<ActionResult<T>> ExecuteAsync<T>(
        Func<Task<ActionResult>> operation,
        string errorMessage,
        string userFriendlyMessage,
        params object[] logParams)
    {
        try
        {
            var result = await operation();
            return (ActionResult<T>)result;
        }
        catch (KeyNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, logParams);
            return new ObjectResult(userFriendlyMessage) { StatusCode = 500 };
        }
    }
}
