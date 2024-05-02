using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microbians.Common.Patterns
{
    public abstract  class AbstractAsyncDecorator : IAsyncDecorator
    {
        private readonly ILogger log;

        protected AbstractAsyncDecorator(ILogger log) => this.log = log;

        public abstract Task ExecuteAsync();
    }

    public abstract class AbstractAsyncDecorator<T> : IAsyncDecorator<T>
    {
        private readonly ILogger log;

        public AbstractAsyncDecorator(ILogger log)
        {
            this.log = log;
        }
        public abstract Task ExecuteAsync(T parameter);
    }
    
    public abstract class AbstractAsyncDecorator<T, TResult> : IAsyncDecorator<T, TResult>
    {
        private readonly ILogger log;

        public AbstractAsyncDecorator(ILogger log)
        {
            this.log = log;
        }
        public abstract Task<TResult> ExecuteAsync(T parameter);
    }
}