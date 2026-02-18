using System.Reflection;
using Aero.Social.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Plugs;

/// <summary>
/// Default implementation of the plug executor
/// </summary>
public class PlugExecutor : IPlugExecutor
{
    private readonly ILogger<PlugExecutor>? _logger;

    public PlugExecutor(ILogger<PlugExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PlugExecutionResult> ExecuteAsync(
        MethodInfo plugMethod,
        ISocialProvider provider,
        PlugExecutionContext context,
        Dictionary<string, object>? fieldValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation(
                "Executing plug {PlugIdentifier} on provider {ProviderIdentifier}",
                plugMethod.Name,
                provider.Identifier);

            // Prepare method parameters
            var parameters = PrepareMethodParameters(plugMethod, context, fieldValues);

            // Invoke the method
            var result = plugMethod.Invoke(provider, parameters);

            // Handle async methods
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Extract result from Task<T>
                var resultProperty = task.GetType().GetProperty("Result");
                var taskResult = resultProperty?.GetValue(task);

                return PlugExecutionResult.SuccessResult(taskResult);
            }

            return PlugExecutionResult.SuccessResult(result);
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            _logger?.LogError(
                tie.InnerException,
                "Plug {PlugIdentifier} threw an exception",
                plugMethod.Name);

            return PlugExecutionResult.FailedResult(
                tie.InnerException.Message,
                tie.InnerException);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Failed to execute plug {PlugIdentifier}",
                plugMethod.Name);

            return PlugExecutionResult.FailedResult(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public PlugValidationResult ValidateFields(PostPlugAttribute attribute, Dictionary<string, object>? fieldValues)
    {
        var result = new PlugValidationResult();

        foreach (var field in attribute.Fields)
        {
            var value = fieldValues?.GetValueOrDefault(field.Name);

            foreach (var validation in field.Validations)
            {
                if (!validation.Validate(value))
                {
                    result.AddError(field.Name, validation.ErrorMessage ?? $"Validation failed for {field.Name}");
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public PlugValidationResult ValidateFields(PlugAttribute attribute, Dictionary<string, object>? fieldValues)
    {
        var result = new PlugValidationResult();

        foreach (var field in attribute.Fields)
        {
            var value = fieldValues?.GetValueOrDefault(field.Name);

            foreach (var validation in field.Validations)
            {
                if (!validation.Validate(value))
                {
                    result.AddError(field.Name, validation.ErrorMessage ?? $"Validation failed for {field.Name}");
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public bool ShouldExecute(PostPlugAttribute attribute, DateTime? lastRunTime, int executionCount)
    {
        // Check if we've exceeded the total runs
        if (attribute.TotalRuns > 0 && executionCount >= attribute.TotalRuns)
        {
            _logger?.LogDebug(
                "Plug {PlugIdentifier} has reached max runs ({ExecutionCount}/{TotalRuns})",
                attribute.Identifier,
                executionCount,
                attribute.TotalRuns);
            return false;
        }

        // Check if enough time has passed since last run
        if (lastRunTime.HasValue)
        {
            var nextRunTime = lastRunTime.Value.AddMilliseconds(attribute.RunEveryMilliseconds);
            if (DateTime.UtcNow < nextRunTime)
            {
                _logger?.LogDebug(
                    "Plug {PlugIdentifier} not ready to run. Next run at {NextRunTime}",
                    attribute.Identifier,
                    nextRunTime);
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool ShouldExecute(PlugAttribute attribute, DateTime? lastRunTime, int executionCount)
    {
        // Check if we've exceeded the total runs
        if (attribute.TotalRuns > 0 && executionCount >= attribute.TotalRuns)
        {
            _logger?.LogDebug(
                "Plug {PlugIdentifier} has reached max runs ({ExecutionCount}/{TotalRuns})",
                attribute.Identifier,
                executionCount,
                attribute.TotalRuns);
            return false;
        }

        // Check if enough time has passed since last run
        if (lastRunTime.HasValue)
        {
            var nextRunTime = lastRunTime.Value.AddMilliseconds(attribute.RunEveryMilliseconds);
            if (DateTime.UtcNow < nextRunTime)
            {
                _logger?.LogDebug(
                    "Plug {PlugIdentifier} not ready to run. Next run at {NextRunTime}",
                    attribute.Identifier,
                    nextRunTime);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Prepares the parameters for invoking a plug method
    /// </summary>
    private object?[] PrepareMethodParameters(
        MethodInfo method,
        PlugExecutionContext context,
        Dictionary<string, object>? fieldValues)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name;

            if (paramName == null) continue;

            // Try to get value from field values first
            if (fieldValues?.TryGetValue(paramName, out var fieldValue) == true)
            {
                args[i] = ConvertValue(fieldValue, param.ParameterType);
            }
            // Then try context data
            else if (context.Data.TryGetValue(paramName, out var contextValue))
            {
                args[i] = ConvertValue(contextValue, param.ParameterType);
            }
            // Use default value if parameter is optional
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            // Handle cancellation token
            else if (param.ParameterType == typeof(CancellationToken))
            {
                args[i] = CancellationToken.None;
            }
            else
            {
                args[i] = null;
            }
        }

        return args;
    }

    /// <summary>
    /// Converts a value to the target type
    /// </summary>
    private object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum && value is string strValue)
        {
            return Enum.Parse(underlyingType, strValue, ignoreCase: true);
        }

        return Convert.ChangeType(value, underlyingType);
    }
}
