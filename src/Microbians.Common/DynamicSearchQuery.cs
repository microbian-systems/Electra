using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microbians.Common.Patterns;

namespace Microbians.Common
{
    /// <summary>
    /// A type that represents a search parameter for IDynamicSearchParam
    /// </summary>
    /// <typeparam name="TParam">Represents a parameter to be consumed by the type IDynamicSearchParam</typeparam>
    public interface IDynamicSearchQueryParam<TParam>
    {
        /// <summary>
        /// represents a LINQ Expression that can be used to query a LINQ enabled data source
        /// </summary>
        Expression<Func<bool, TParam>> Filter { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Default search parameter for <c>IDynamicSearchQueryParam</c>
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    public class DynamicSearchParam<TParam> : IDynamicSearchQueryParam<TParam>
    {
        /// <summary>
        /// Constructor for dynamicSearchParam
        /// </summary>
        /// <param name="filter">Represents a LINQ expression to filter the result set</param>
        public DynamicSearchParam(Expression<Func<bool, TParam>> filter) => Filter = filter;
        
        /// <inheritdoc />
        /// <summary>
        /// Represents a LINQ expression to filter the result set
        /// </summary>
        public Expression<Func<bool, TParam>> Filter { get; }
    }

    // todo - complete documentation for Microbians.Core.DynamicSearchQueryBase
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public abstract class DynamicSearchQueryBase<TParam, TResult> : IAsyncQueryHandler<IDynamicSearchQueryParam<TParam>, TResult>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public abstract Task<TResult> ExecuteAsync(IDynamicSearchQueryParam<TParam> query);
    }
}