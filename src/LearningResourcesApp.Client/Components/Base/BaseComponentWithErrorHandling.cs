using Microsoft.AspNetCore.Components;

namespace LearningResourcesApp.Client.Components.Base;

public abstract class BaseComponentWithErrorHandling : ComponentBase
{
    protected string foutmelding = string.Empty;
    protected string succesmelding = string.Empty;
    protected bool isBezig = false;

    /// <summary>
    /// Executes an async action with error handling
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action, string? customErrorMessage = null)
    {
        try
        {
            foutmelding = string.Empty;
            await action();
        }
        catch (Exception ex)
        {
            foutmelding = customErrorMessage ?? $"Er is een fout opgetreden: {ex.Message}";
        }
    }

    /// <summary>
    /// Executes an async action with error handling and loading state
    /// </summary>
    protected async Task ExecuteWithLoadingAsync(Func<Task> action, string? customErrorMessage = null)
    {
        try
        {
            isBezig = true;
            foutmelding = string.Empty;
            await action();
        }
        catch (Exception ex)
        {
            foutmelding = customErrorMessage ?? $"Er is een fout opgetreden: {ex.Message}";
        }
        finally
        {
            isBezig = false;
        }
    }

    /// <summary>
    /// Executes an async action with error handling and success message
    /// </summary>
    protected async Task ExecuteWithSuccessAsync(Func<Task> action, string successMessage, string? customErrorMessage = null)
    {
        try
        {
            foutmelding = string.Empty;
            succesmelding = string.Empty;
            await action();
            succesmelding = successMessage;
        }
        catch (Exception ex)
        {
            foutmelding = customErrorMessage ?? $"Er is een fout opgetreden: {ex.Message}";
        }
    }

    /// <summary>
    /// Executes an async action with error handling, loading state, and returns a result
    /// </summary>
    protected async Task<T?> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? customErrorMessage = null)
    {
        try
        {
            isBezig = true;
            foutmelding = string.Empty;
            return await action();
        }
        catch (Exception ex)
        {
            foutmelding = customErrorMessage ?? $"Er is een fout opgetreden: {ex.Message}";
            return default;
        }
        finally
        {
            isBezig = false;
        }
    }

    /// <summary>
    /// Clears error message
    /// </summary>
    protected void ClearError()
    {
        foutmelding = string.Empty;
    }

    /// <summary>
    /// Clears success message
    /// </summary>
    protected void ClearSuccess()
    {
        succesmelding = string.Empty;
    }
}
