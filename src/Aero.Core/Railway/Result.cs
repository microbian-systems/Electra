namespace Aero.Core.Railway;

/// <summary>
/// Represents a computation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="TError">The type of the error value in case of failure.</typeparam>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <remarks>
/// <para>
/// The Result type is the cornerstone of Railway-Oriented Programming, a functional programming pattern
/// that models operations as a railway track with two parallel rails: one for success and one for failure.
/// </para>
/// <para>
/// <b>Railway-Oriented Programming:</b>
/// Imagine a railway track where the "success rail" carries valid values forward through the pipeline,
/// while the "failure rail" bypasses all subsequent operations. This allows for elegant error handling
/// without try-catch blocks scattered throughout the code.
/// </para>
/// <para>
/// <b>Monadic Lifting:</b>
/// The Result type enables "lifting" ordinary functions into the monadic context. When you lift a function
/// <c>T -&gt; U</c> using <see cref="ResultExtensions.Map{TError, TValue, TOut}"/> or 
/// <see cref="ResultExtensions.Bind{TError, TValue, TOut}"/>,
/// you transform it to operate on <c>Result&lt;TError, T&gt;</c>, handling the success/failure states automatically.
/// This is analogous to how LINQ's Select (map) and SelectMany (bind) lift operations into the enumerable context.
/// </para>
/// <para>
/// The type is implemented as a discriminated union with two cases: <see cref="Ok"/> for success
/// and <see cref="Failure"/> for failure. Implicit operators allow seamless conversion from values and errors.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating Results
/// Result&lt;string, int&gt; success = 42;           // Implicitly creates Ok(42)
/// Result&lt;string, int&gt; failure = "Not found";  // Implicitly creates Failure("Not found")
/// 
/// // Railway-oriented flow
/// var result = GetUserById(123)
///     .Bind(user => ValidateUser(user))
///     .Map(validUser => validUser.Email)
///     .Match(
///         email => $"Found: {email}",
///         error => $"Error: {error}"
///     );
/// </code>
/// </example>
public abstract record Result<TError, TValue>
{
    /// <summary>
    /// Represents a successful result containing a value.
    /// </summary>
    /// <param name="Value">The success value.</param>
    /// <remarks>
    /// The Ok case represents the "success rail" in railway-oriented programming.
    /// When a computation succeeds, it wraps the result value in this case.
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;string, User&gt; result = new Result&lt;string, User&gt;.Ok(user);
    /// // Or using implicit conversion:
    /// Result&lt;string, User&gt; result2 = user;
    /// </code>
    /// </example>
    public sealed record Ok(TValue Value) : Result<TError, TValue>;

    /// <summary>
    /// Represents a failed result containing an error.
    /// </summary>
    /// <param name="Error">The error value describing what went wrong.</param>
    /// <remarks>
    /// The Failure case represents the "failure rail" in railway-oriented programming.
    /// Once a computation fails and enters this rail, subsequent operations are bypassed
    /// until explicitly handled (e.g., via Match or GetOrElse).
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;string, User&gt; result = new Result&lt;string, User&gt;.Failure("User not found");
    /// // Or using implicit conversion:
    /// Result&lt;string, User&gt; result2 = "User not found";
    /// </code>
    /// </example>
    public sealed record Failure(TError Error) : Result<TError, TValue>;
    
    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="TValue"/> to a <see cref="Ok"/> result.
    /// </summary>
    /// <param name="value">The value to wrap in a success result.</param>
    /// <returns>A new <see cref="Ok"/> instance containing the value.</returns>
    /// <remarks>
    /// This operator enables the "lifting" of plain values into the Result context.
    /// Instead of explicitly writing <c>new Result&lt;TError, TValue&gt;.Ok(value)</c>,
    /// you can simply assign the value directly.
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;string, int&gt; result = 42;  // Implicitly converted to Ok(42)
    /// </code>
    /// </example>
    public static implicit operator Result<TError, TValue>(TValue value) => new Ok(value);

    /// <summary>
    /// Implicitly converts an error of type <typeparamref name="TError"/> to a <see cref="Failure"/> result.
    /// </summary>
    /// <param name="error">The error to wrap in a failure result.</param>
    /// <returns>A new <see cref="Failure"/> instance containing the error.</returns>
    /// <remarks>
    /// This operator allows direct assignment of errors to Result variables,
    /// automatically wrapping them in a Failure case.
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;string, int&gt; result = "Invalid input";  // Implicitly converted to Failure("Invalid input")
    /// </code>
    /// </example>
    public static implicit operator Result<TError, TValue>(TError error) => new Failure(error);

    /// <summary>
    /// Explicitly unwraps a <see cref="Result{TError, TValue}"/> to its underlying value.
    /// </summary>
    /// <param name="result">The result to unwrap.</param>
    /// <returns>The success value if the result is <see cref="Ok"/>.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when the result is a <see cref="Failure"/>. The exception message includes the error.
    /// </exception>
    /// <remarks>
    /// This explicit cast allows forceful extraction of the success value, but should be used
    /// with caution as it throws an exception on failure. Consider using <see cref="ResultExtensions.Match{TError, TValue, T}"/>,
    /// <see cref="ResultExtensions.GetOrElse{TError, TValue}(Result{TError, TValue}, TValue)"/>, or
    /// <see cref="ResultExtensions.GetOrThrow{TError, TValue}(Result{TError, TValue}, string?)"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;string, int&gt; result = 42;
    /// int value = (int)result;  // value is 42
    /// 
    /// Result&lt;string, int&gt; failure = "Error";
    /// int value2 = (int)failure;  // Throws InvalidCastException
    /// </code>
    /// </example>
    public static explicit operator TValue(Result<TError, TValue> result) =>
        result switch
        {
            Ok(var value) => value,
            Failure(var error) => throw new InvalidCastException($"Result was Failure: {error}"),
        };

    /// <summary>
    /// Explicitly unwraps a <see cref="Result{TError, TValue}"/> to its underlying error.
    /// </summary>
    /// <param name="result">The result to unwrap.</param>
    /// <returns>The error value if the result is <see cref="Failure"/>.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when the result is an <see cref="Ok"/>. The exception message includes the value.
    /// </exception>
    /// <remarks>
    /// This explicit cast allows forceful extraction of the error value, useful for error inspection
    /// in test scenarios or when you need to branch on the specific error type after pattern matching.
    /// </remarks>
    /// <example>
    /// <code>
    /// Result&lt;string, int&gt; failure = "Not found";
    /// string error = (string)failure;  // error is "Not found"
    /// 
    /// Result&lt;string, int&gt; result = 42;
    /// string error2 = (string)result;  // Throws InvalidCastException
    /// </code>
    /// </example>
    public static explicit operator TError(Result<TError, TValue> result) =>
        result switch
        {
            Failure(var error) => error,
            Ok(var value) => throw new InvalidCastException($"Result was Ok: {value}"),
        };
}

/// <summary>
/// Represents an optional value that may or may not be present.
/// </summary>
/// <typeparam name="T">The type of the value that may be present.</typeparam>
/// <remarks>
/// <para>
/// Option is a functional programming pattern for handling values that may be absent,
/// providing a safer alternative to null references. It is conceptually similar to nullable types
/// but with explicit handling of the "none" case through the type system.
/// </para>
/// <para>
/// <b>Monadic Lifting:</b>
    /// The Option type enables "lifting" ordinary functions into the optional context.
    /// When you lift a function <c>T -&gt; U</c> using <see cref="OptionExtensions.Map{TIn, TOut}"/>,
    /// it only applies when the Option is <see cref="Some"/>; if it's <see cref="None"/>,
    /// the function is skipped and None propagates forward automatically.
    /// </para>
/// <para>
/// The type is implemented as a discriminated union with two cases: <see cref="Some"/> when a value exists
/// and <see cref="None"/> when it does not. Implicit conversion from <c>T</c> automatically handles
/// null checking, converting null values to <see cref="None"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating Options
/// Option&lt;string&gt; some = "Hello";     // Implicitly creates Some("Hello")
/// Option&lt;string&gt; none = null;        // Implicitly creates None
/// 
/// // Chaining operations
/// var result = FindUserByEmail("user@example.com")
///     .Map(user => user.Name.ToUpper())
///     .GetOrElse("UNKNOWN");
/// </code>
/// </example>
public abstract record Option<T>
{
    /// <summary>
    /// Represents an Option that contains a value.
    /// </summary>
    /// <param name="Value">The contained value, guaranteed to be non-null.</param>
    /// <remarks>
    /// The Some case represents the presence of a value. Unlike nullable types,
    /// when you have a Some, you are guaranteed that the value is not null.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;int&gt; option = new Option&lt;int&gt;.Some(42);
    /// // Or using implicit conversion:
    /// Option&lt;int&gt; option2 = 42;
    /// </code>
    /// </example>
    public sealed record Some(T Value) : Option<T>;

    /// <summary>
    /// Represents an Option that has no value.
    /// </summary>
    /// <remarks>
    /// The None case represents the absence of a value, providing a type-safe alternative to null.
    /// When operations are chained on a None, they are automatically bypassed and None propagates forward.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;string&gt; option = new Option&lt;string&gt;.None();
    /// // Or using implicit conversion with null:
    /// Option&lt;string&gt; option2 = null;
    /// </code>
    /// </example>
    public sealed record None : Option<T>;
    
    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> to an Option.
    /// </summary>
    /// <param name="value">The value to convert. If null, creates <see cref="None"/>; otherwise, creates <see cref="Some"/>.</param>
    /// <returns>
    /// A <see cref="Some"/> containing the value if it is not null; otherwise, <see cref="None"/>.
    /// </returns>
    /// <remarks>
    /// This operator implements null-to-None coercion, making Option a drop-in replacement
    /// for nullable reference types. It enables the "lifting" of potentially null values into
    /// the Option monadic context safely.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;string&gt; option1 = "Hello";  // Some("Hello")
    /// Option&lt;string&gt; option2 = null;     // None
    /// Option&lt;int?&gt; option3 = 42;         // Some(42)
    /// Option&lt;int?&gt; option4 = null;       // None
    /// </code>
    /// </example>
    public static implicit operator Option<T>(T value) =>
        value is not null ? new Some(value) : new None();

    /// <summary>
    /// Explicitly unwraps an Option to its underlying value.
    /// </summary>
    /// <param name="option">The Option to unwrap.</param>
    /// <returns>The contained value if the Option is <see cref="Some"/>.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when the Option is <see cref="None"/>, indicating there is no value to extract.
    /// </exception>
    /// <remarks>
    /// This explicit cast allows forceful extraction of the value, but should be used with caution
    /// as it throws an exception when the Option is None. Prefer using 
    /// <see cref="OptionExtensions.GetOrElse{T}(Option{T}, T)"/>, pattern matching, or
    /// <see cref="OptionExtensions.Map{TIn, TOut}"/> for safer handling.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;int&gt; option = 42;
    /// int value = (int)option;  // value is 42
    /// 
    /// Option&lt;int&gt; none = new Option&lt;int&gt;.None();
    /// int value2 = (int)none;   // Throws InvalidCastException
    /// </code>
    /// </example>
    public static explicit operator T(Option<T> option) =>
        option switch
        {
            Some(var value) => value,
            None => throw new InvalidCastException("Cannot cast None to value"),
        };
}

/// <summary>
/// Represents the absence of a value, used primarily for type inference in generic contexts.
/// </summary>
/// <remarks>
/// <para>
/// This is a sentinel type used when you need to represent "no value" in a type-safe way
/// without specifying the type parameter. It is primarily used with the <see cref="Option{T}"/>
/// type and the <see cref="Prelude.None"/> factory method.
/// </para>
/// <para>
/// Since it's a struct with no fields, it has zero memory overhead and serves purely as a
/// type marker in the functional programming abstractions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Used implicitly through Prelude
/// Option&lt;string&gt; option = Prelude.None;
/// </code>
/// </example>
public readonly struct None { }
