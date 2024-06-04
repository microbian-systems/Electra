namespace Electra.Common.Patterns
{
    /// <summary>
    /// Represents a Query Request in a CQS model
    /// </summary>
    /// <typeparam name="TResult">They type to be returned</typeparam>
    public interface IAsyncQueryHandler<TResult>
    {
        /// <summary>
        /// Execute the Query asynchronously
        /// </summary>
        /// <returns>The <typeparam name="TResult"> to be expected</typeparam></returns>
        Task<TResult> ExecuteAsync();
    }

    /// <summary>
    /// Represents a Query Request in a CQS model that accepts a parameter
    /// </summary>
    /// <typeparam name="TParam">Represents the parameter type to be consumed</typeparam>
    /// <typeparam name="TResult">They type to be returned</typeparam>
    /// <remarks>Since a Query object represents a specific action there is no base type, contract or interface for <typeparamref name="TParam"/></remarks>
    /// <remarks>The <typeparam name="TParam"></typeparam>param name="TParam">that represents an IQuery&lt;T&gt; in traditional CQS views</remarks>
    public interface IAsyncQueryHandler<in TParam, TResult> //where TParam : IQuery<TResult>
    {
        /// <summary>
        /// Execute the Query asynchronously
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        Task<TResult> ExecuteAsync(TParam param);
    }

    public interface IQueryHandler<TResult>
    {
        TResult Execute();
    }

    public interface IQueryHandler<TParam, TResult>
    {
        TResult Execute(TParam param);
    }
}