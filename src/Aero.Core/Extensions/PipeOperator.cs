namespace Aero.Core.Extensions;


/// <summary>
/// The pipe operator extension allows for a more functional programming style by enabling the use of the pipe operator (|) to pass an object through a series of functions.
/// <remarks>example usage: var result = 5 | (x => x * 2) | (x => x.ToString()); // result will be "10"</remarks>
/// </summary>
public static class PipeOperator
{
    extension<T, TResult>(T)
    {
        /// <summary>
        /// Invokes the specified function with the given source value and returns the result. Enables pipeline-style
        /// function application using the bitwise OR operator.
        /// </summary>
        /// <remarks>
        /// This operator overload allows for more readable, pipeline-style code by enabling the
        /// use of the bitwise OR operator to apply a function to a value. For example, 'value | func' is equivalent to
        /// 'func(value)'
        /// </remarks>
        /// <param name="source">The value to be passed as an argument to the function.</param>
        /// <param name="func">A function to apply to the source value. Cannot be null.</param>
        /// <returns>The result of invoking the specified function with the source value.</returns>
        /// <example>
        /// var result = "5"
        ///             | int.Parse
        ///             | (x => x * 2) 
        ///             | (x => x.ToString());
        /// </example>
        public static TResult operator |(T source, Func<T, TResult> func) => func(source);
    }
}

