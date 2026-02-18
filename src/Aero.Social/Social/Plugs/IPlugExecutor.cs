using System.Reflection;
using System.Runtime.CompilerServices;
using Aero.Social.Abstractions;

namespace Aero.Social.Plugs;

/// <summary>
/// Interface for executing plug methods with scheduling and validation
/// </summary>
public interface IPlugExecutor
{
    /// <summary>
    /// Executes a plug method with the given context and parameters
    /// </summary>
    /// <param name="plugMethod">The method info of the plug to execute</param>
    /// <param name="provider">The provider instance containing the plug method</param>
    /// <param name="context">The execution context for the plug</param>
    /// <param name="fieldValues">The field values configured for this plug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the plug execution</returns>
    Task<PlugExecutionResult> ExecuteAsync(
        MethodInfo plugMethod,
        ISocialProvider provider,
        PlugExecutionContext context,
        Dictionary<string, object>? fieldValues = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the field values against the plug's field definitions
    /// </summary>
    /// <param name="attribute">The plug attribute containing field definitions</param>
    /// <param name="fieldValues">The values to validate</param>
    /// <returns>Validation result with any errors</returns>
    PlugValidationResult ValidateFields(PostPlugAttribute attribute, Dictionary<string, object>? fieldValues);

    /// <summary>
    /// Validates the field values against the plug's field definitions
    /// </summary>
    /// <param name="attribute">The plug attribute containing field definitions</param>
    /// <param name="fieldValues">The values to validate</param>
    /// <returns>Validation result with any errors</returns>
    PlugValidationResult ValidateFields(PlugAttribute attribute, Dictionary<string, object>? fieldValues);

    /// <summary>
    /// Checks if a plug should be executed based on its schedule and run count
    /// </summary>
    /// <param name="attribute">The plug attribute</param>
    /// <param name="lastRunTime">The last time the plug was executed</param>
    /// <param name="executionCount">The number of times the plug has been executed</param>
    /// <returns>True if the plug should be executed</returns>
    bool ShouldExecute(PostPlugAttribute attribute, DateTime? lastRunTime, int executionCount);

    /// <summary>
    /// Checks if a plug should be executed based on its schedule and run count
    /// </summary>
    /// <param name="attribute">The plug attribute</param>
    /// <param name="lastRunTime">The last time the plug was executed</param>
    /// <param name="executionCount">The number of times the plug has been executed</param>
    /// <returns>True if the plug should be executed</returns>
    bool ShouldExecute(PlugAttribute attribute, DateTime? lastRunTime, int executionCount);
}

/// <summary>
/// Context provided to a plug during execution
/// </summary>
public class PlugExecutionContext
{
    /// <summary>
    /// The integration ID
    /// </summary>
    public string IntegrationId { get; set; } = string.Empty;

    /// <summary>
    /// The access token for the provider
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The post ID if this is a post-specific plug
    /// </summary>
    public string? PostId { get; set; }

    /// <summary>
    /// Additional data that may be needed for execution
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// The time when the plug was scheduled to run
    /// </summary>
    public DateTime ScheduledTime { get; set; }
}

/// <summary>
/// Result of a plug execution
/// </summary>
public class PlugExecutionResult
{
    /// <summary>
    /// Whether the execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The result data from the execution
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Any error message if the execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The exception if one occurred
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Whether the plug should be rescheduled
    /// </summary>
    public bool ShouldReschedule { get; set; } = true;

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static PlugExecutionResult SuccessResult(object? data = null) => new()
    {
        Success = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static PlugExecutionResult FailedResult(string errorMessage, Exception? exception = null) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        Exception = exception
    };
}

/// <summary>
/// Result of field validation
/// </summary>
public class PlugValidationResult
{
    /// <summary>
    /// Whether all fields are valid
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Dictionary of field names to error messages
    /// </summary>
    public Dictionary<string, List<string>> Errors { get; set; } = new();

    /// <summary>
    /// Adds an error for a specific field
    /// </summary>
    public void AddError(string fieldName, string error)
    {
        if (!Errors.ContainsKey(fieldName))
        {
            Errors[fieldName] = new List<string>();
        }
        Errors[fieldName].Add(error);
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static PlugValidationResult Valid() => new();
}
