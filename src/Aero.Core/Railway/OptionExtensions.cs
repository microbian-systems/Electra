namespace Aero.Core.Railway;

/// <summary>
/// Provides extension methods for working with <see cref="Option{T}"/> types,
/// enabling functional programming patterns for optional values.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the core functional operations for the Option type,
/// following the same monadic patterns as Result but for the simpler case of
/// optional values (presence/absence rather than success/failure).
/// </para>
/// <para>
/// <b>Monadic Lifting with Option:</b>
/// The Option type provides two fundamental operations that "lift" functions into
/// the optional context:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Map (Functor lift):</b> Transforms <c>T -&gt; U</c> to operate on 
/// <c>Option&lt;T&gt; -&gt; Option&lt;U&gt;</c>. If Some, the function is applied;
/// if None, None propagates unchanged.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Bind (Monadic lift):</b> Transforms <c>T -&gt; Option&lt;U&gt;</c> to operate on
/// <c>Option&lt;T&gt; -&gt; Option&lt;U&gt;</c>, flattening nested Options automatically.
/// </description>
/// </item>
/// </list>
/// <para>
/// These operations allow you to compose computations on optional values without
/// explicit null checks, following the "lift and compose" pattern common in functional programming.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Composing operations on optional values
/// var name = FindUserById(123)
///     .Bind(user => user.MiddleName)  // MiddleName is Option&lt;string&gt;
///     .Map(name => name.ToUpper())
///     .GetOrElse("No middle name");
/// </code>
/// </example>
public static class OptionExtensions
{
    /// <summary>
    /// Transforms the value inside an Option using the provided function.
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="option">The Option to transform.</param>
    /// <param name="f">The function to apply to the contained value.</param>
    /// <returns>
    /// An Option containing the transformed value if the input was Some;
    /// otherwise, None.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Map implements the functor pattern for Option. It "lifts" a plain function
    /// <c>TIn -&gt; TOut</c> to operate on <c>Option&lt;TIn&gt; -&gt; Option&lt;TOut&gt;</c>.
    /// </para>
    /// <para>
    /// This is the Option equivalent of Enumerable.Select. It allows you to transform
    /// values without worrying about null checks - if the Option is None, the function
    /// is never called and None propagates forward automatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;int&gt; option = 42;
    /// Option&lt;string&gt; mapped = option.Map(n => $"Value: {n}");
    /// // mapped is Some("Value: 42")
    /// 
    /// Option&lt;int&gt; none = new Option&lt;int&gt;.None();
    /// Option&lt;string&gt; mapped2 = none.Map(n => $"Value: {n}");
    /// // mapped2 is None - the function was never called
    /// </code>
    /// </example>
    public static Option<TOut> Map<TIn, TOut>(
        this Option<TIn> option, Func<TIn, TOut> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => new Option<TOut>.Some(f(value)),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    /// <summary>
    /// Chains an Option-returning function to the value inside an Option, flattening nested Options.
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="option">The Option to chain from.</param>
    /// <param name="f">The function that returns an Option.</param>
    /// <returns>
    /// The Option returned by <paramref name="f"/> if the input was Some;
    /// otherwise, None.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Bind implements the monadic pattern for Option. Unlike Map which would create
    /// <c>Option&lt;Option&lt;T&gt;&gt;</c>, Bind flattens the structure to just <c>Option&lt;T&gt;</c>.
    /// </para>
    /// <para>
    /// This is essential for chaining operations where each step may or may not return a value.
    /// It's the Option equivalent of Enumerable.SelectMany (monadic bind).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;User&gt; user = FindUser(1);
    /// Option&lt;Address&gt; address = user.Bind(u => u.Address);  // Address is Option&lt;Address&gt;
    /// // If user is None, address is None without calling the bind function
    /// // If user is Some, we get their address (which may also be None)
    /// </code>
    /// </example>
    public static Option<TOut> Bind<TIn, TOut>(
        this Option<TIn> option, Func<TIn, Option<TOut>> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => f(value),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    /// <summary>
    /// Asynchronously transforms the value inside an Option using the provided async function.
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="option">The Option to transform.</param>
    /// <param name="f">The async function to apply to the contained value.</param>
    /// <returns>
    /// A task that resolves to an Option containing the transformed value if the input was Some;
    /// otherwise, None.
    /// </returns>
    /// <remarks>
    /// This is the asynchronous version of <see cref="Map{TIn, TOut}"/>.
    /// It enables lifting async operations into the Option context, which is useful
    /// for I/O-bound operations that may or may not return values.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;int&gt; userId = 42;
    /// Option&lt;UserProfile&gt; profile = await userId.MapAsync(async id =>
    ///     await userService.GetProfileAsync(id));
    /// // If userId is Some, the async lookup is performed
    /// // If userId is None, returns None immediately without awaiting
    /// </code>
    /// </example>
    public static async Task<Option<TOut>> MapAsync<TIn, TOut>(
        this Option<TIn> option, Func<TIn, Task<TOut>> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => new Option<TOut>.Some(await f(value)),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    /// <summary>
    /// Asynchronously chains an Option-returning function to the value inside an Option.
    /// </summary>
    /// <typeparam name="TIn">The type of the input value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="option">The Option to chain from.</param>
    /// <param name="f">The async function that returns an Option.</param>
    /// <returns>
    /// A task that resolves to the Option returned by <paramref name="f"/> if the input was Some;
    /// otherwise, None.
    /// </returns>
    /// <remarks>
    /// This is the asynchronous version of <see cref="Bind{TIn, TOut}"/>.
    /// It enables chaining async operations that return Options, flattening the result automatically.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;string&gt; email = user.Email;  // Option&lt;string&gt;
    /// Option&lt;EmailDetails&gt; details = await email.BindAsync(async e =>
    ///     await emailService.LookupDetailsAsync(e));
    /// </code>
    /// </example>
    public static async Task<Option<TOut>> BindAsync<TIn, TOut>(
        this Option<TIn> option, Func<TIn, Task<Option<TOut>>> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => await f(value),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    /// <summary>
    /// Converts an Option to a Result, using the provided error if the Option is None.
    /// </summary>
    /// <typeparam name="TError">The type of the error to use if None.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="option">The Option to convert.</param>
    /// <param name="error">The error value to use if the Option is None.</param>
    /// <returns>
    /// An Ok Result containing the value if the Option was Some;
    /// a Failure Result containing the error if the Option was None.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method serves as a bridge between the Option and Result types. It converts
    /// the "absence of value" concept (Option) into the "failure with reason" concept (Result).
    /// </para>
    /// <para>
    /// This is useful when you need to provide more context about why a value is missing,
    /// or when integrating Option-based code with Result-based error handling.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;User&gt; user = FindUser(123);
    /// Result&lt;string, User&gt; result = user.OkOrFailure("User not found");
    /// // If user is Some, result is Ok(user)
    /// // If user is None, result is Failure("User not found")
    /// 
    /// // Now you can use Result operations
    /// var email = result.Map(u => u.Email)
    ///     .Match(
    ///         e => e,
    ///         err => $"Error: {err}"
    ///     );
    /// </code>
    /// </example>
    public static Result<TError, TValue> OkOrFailure<TError, TValue>(
        this Option<TValue> option, TError error) =>
        option switch
        {
            Option<TValue>.Some(var value) => new Result<TError, TValue>.Ok(value),
            Option<TValue>.None => new Result<TError, TValue>.Failure(error),
        };

    /// <summary>
    /// Filters an Option based on a predicate, returning None if the predicate returns false.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="option">The Option to filter.</param>
    /// <param name="predicate">The condition that the value must satisfy.</param>
    /// <returns>
    /// The original Option if it was Some and the predicate returns true;
    /// None if the Option was Some but the predicate returns false;
    /// None if the Option was already None.
    /// </returns>
    /// <remarks>
    /// Filter allows you to add validation constraints to Options. It's useful for
    /// refining an Option based on runtime conditions without breaking the fluent chain.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;int&gt; option = 42;
    /// Option&lt;int&gt; filtered = option.Filter(n => n > 50);
    /// // filtered is None because 42 is not > 50
    /// 
    /// Option&lt;int&gt; option2 = 75;
    /// Option&lt;int&gt; filtered2 = option2.Filter(n => n > 50);
    /// // filtered2 is Some(75)
    /// 
    /// Option&lt;User&gt; user = FindUser(1);
    /// Option&lt;User&gt; activeUser = user.Filter(u => u.IsActive);
    /// // Only Some if user exists AND is active
    /// </code>
    /// </example>
    public static Option<T> Filter<T>(
        this Option<T> option, Func<T, bool> predicate) =>
        option switch
        {
            Option<T>.Some(var value) when predicate(value) => option,
            Option<T>.Some => new Option<T>.None(),
            Option<T>.None => option,
        };

    /// <summary>
    /// Returns the value inside the Option if it is Some; otherwise, returns the provided fallback value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="option">The Option to extract from.</param>
    /// <param name="fallback">The value to return if the Option is None.</param>
    /// <returns>
    /// The contained value if the Option is Some; otherwise, <paramref name="fallback"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// GetOrElse provides a way to extract a value from an Option with a default.
    /// Unlike the explicit cast operator, this never throws - it guarantees a value.
    /// </para>
    /// <para>
    /// This is typically used at the end of an Option pipeline when you need an actual value
    /// rather than remaining in the optional context.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;string&gt; name = "Alice";
    /// string value = name.GetOrElse("Unknown");
    /// // value is "Alice"
    /// 
    /// Option&lt;string&gt; none = new Option&lt;string&gt;.None();
    /// string value2 = none.GetOrElse("Unknown");
    /// // value2 is "Unknown"
    /// 
    /// // Common pattern: providing defaults for configuration
    /// int timeout = config.GetTimeoutOption().GetOrElse(30);  // Default 30 seconds
    /// </code>
    /// </example>
    public static T GetOrElse<T>(this Option<T> option, T fallback) =>
        option switch
        {
            Option<T>.Some(var value) => value,
            Option<T>.None => fallback,
        };

    /// <summary>
    /// Returns the value inside the Option if it is Some; otherwise, returns the result of the fallback function.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="option">The Option to extract from.</param>
    /// <param name="fallback">A function that returns a fallback value.</param>
    /// <returns>
    /// The contained value if the Option is Some;
    /// otherwise, the result of calling <paramref name="fallback"/>.
    /// </returns>
    /// <remarks>
    /// This overload allows the fallback to be computed lazily. This is useful when the
    /// default value is expensive to compute or when it shouldn't be computed unless needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;string&gt; cachedValue = cache.Get("key");
    /// string value = cachedValue.GetOrElse(() =>
    /// {
    ///     // This expensive computation only runs if cache miss
    ///     var result = ExpensiveDatabaseQuery();
    ///     cache.Set("key", result);
    ///     return result;
    /// });
    /// </code>
    /// </example>
    public static T GetOrElse<T>(this Option<T> option, Func<T> fallback) =>
        option switch
        {
            Option<T>.Some(var value) => value,
            Option<T>.None => fallback(),
        };
}
