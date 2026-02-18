namespace Aero.Core.Railway;

/// <summary>
/// Represents a sequential pipeline of asynchronous operations that can succeed or fail,
/// implementing the Railway-Oriented Programming pattern for complex workflows.
/// </summary>
/// <typeparam name="TInput">The type of the initial input to the pipeline.</typeparam>
/// <typeparam name="TError">The type of error that can occur.</typeparam>
/// <typeparam name="TOutput">The type of the final output.</typeparam>
/// <remarks>
/// <para>
/// The Pipeline class provides a higher-level abstraction for composing multiple operations
/// that each return a <see cref="Result{TError, TOutput}"/>. It maintains the railway pattern
/// across the entire pipeline: once any step fails, subsequent steps are bypassed.
/// </para>
/// <para>
/// <b>Railway-Oriented Programming with Pipelines:</b>
/// While the extension methods on Result allow chaining operations directly, the Pipeline class
/// is useful when you need to:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Dynamically build a sequence of steps (e.g., based on configuration)</description>
/// </item>
/// <item>
/// <description>Reuse the same pipeline with different inputs</description>
/// </item>
/// <item>
/// <description>Create complex workflows with many steps in a declarative way</description>
/// </item>
/// </list>
/// <para>
/// <b>Monadic Composition:</b>
/// Each step added via <see cref="AddStep"/> is "lifted" into the pipeline context. The pipeline
/// automatically handles the monadic binding between steps - you add plain functions that return
/// Results, and the pipeline wires them together with the appropriate Bind operations.
/// </para>
/// <para>
/// Note: All steps in the pipeline share the same input type <typeparamref name="TInput"/>
/// and return the same <c>Result&lt;TError, TOutput&gt;</c> type. For more flexible pipelines,
/// consider using the extension methods directly which allow type transformations between steps.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Building a reusable processing pipeline
/// var orderPipeline = new Pipeline&lt;OrderRequest, string, ProcessedOrder&gt;()
///     .AddStep(req => ValidateOrder(req))
///     .AddStep(req => CheckInventory(req))
///     .AddStep(req => ProcessPayment(req))
///     .AddStep(req => FulfillOrder(req));
/// 
/// // Executing with different inputs
/// var result1 = await orderPipeline.Execute(orderRequest1);
/// var result2 = await orderPipeline.Execute(orderRequest2);
/// 
/// // The pipeline handles the railway automatically:
/// // - If validation fails, inventory check is skipped
/// // - If payment fails, fulfillment is skipped
/// // - Only the first error is returned
/// </code>
/// </example>
public class Pipeline<TInput, TError, TOutput>
{
    private readonly List<Func<TInput, Task<Result<TError, TOutput>>>> _steps = new();

    /// <summary>
    /// Adds an asynchronous step to the pipeline.
    /// </summary>
    /// <param name="step">
    /// A function that takes the pipeline input and returns a Task of Result.
    /// </param>
    /// <returns>The pipeline instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Each step added represents one station on the railway. The step receives the original
    /// pipeline input (not the output of the previous step), making this suitable for workflows
    /// where each operation is independent but must all succeed.
    /// </para>
    /// <para>
    /// For dependent operations (where step N+1 needs the output of step N), use the
    /// <see cref="ResultExtensions.Bind{TError, TValue, TOut}"/> extension methods directly
    /// instead of this Pipeline class.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pipeline = new Pipeline&lt;UserRegistration, string, RegistrationResult&gt;()
    ///     .AddStep(reg => ValidateEmailAsync(reg))
    ///     .AddStep(reg => CheckUsernameAsync(reg))
    ///     .AddStep(reg => CreateAccountAsync(reg))
    ///     .AddStep(reg => SendWelcomeEmailAsync(reg));
    /// </code>
    /// </example>
    public Pipeline<TInput, TError, TOutput> AddStep(Func<TInput, Task<Result<TError, TOutput>>> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Executes all steps in the pipeline sequentially, returning the first failure or the final success.
    /// </summary>
    /// <param name="input">The input to pass to each step in the pipeline.</param>
    /// <returns>
    /// A task that resolves to the first <see cref="Result{TError, TOutput}.Failure"/> encountered,
    /// or the final <see cref="Result{TError, TOutput}.Ok"/> if all steps succeed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Execute runs the pipeline following the railway pattern:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>Start with a default Ok result</description>
    /// </item>
    /// <item>
    /// <description>For each step, execute it with the input</description>
    /// </item>
    /// <item>
    /// <description>If the result is Failure, immediately return it (short-circuit)</description>
    /// </item>
    /// <item>
    /// <description>If all steps succeed, return the last result</description>
    /// </item>
    /// </list>
    /// <para>
    /// This provides automatic error handling - you don't need to check each step manually.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pipeline = new Pipeline&lt;Document, ValidationError, ProcessedDocument&gt;()
    ///     .AddStep(doc => ValidateFormatAsync(doc))
    ///     .AddStep(doc => ScanForVirusesAsync(doc))
    ///     .AddStep(doc => ExtractMetadataAsync(doc))
    ///     .AddStep(doc => StoreDocumentAsync(doc));
    /// 
    /// var result = await pipeline.Execute(myDocument);
    /// 
    /// var message = result.Match(
    ///     processed => $"Document stored: {processed.Id}",
    ///     error => $"Failed at step: {error.Step} - {error.Message}"
    /// );
    /// </code>
    /// </example>
    public async Task<Result<TError, TOutput>> Execute(TInput input)
    {
        Result<TError, TOutput> current = new Result<TError, TOutput>.Ok(default!);

        foreach (var step in _steps)
        {
            current = await step(input);
            if (current is Result<TError, TOutput>.Failure)
                return current;
        }

        return current;
    }
}
