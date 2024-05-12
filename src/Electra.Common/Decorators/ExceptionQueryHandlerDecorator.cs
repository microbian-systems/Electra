using System;
using System.Threading.Tasks;
using Electra.Common.Patterns;
using Microsoft.Extensions.Logging;

namespace Electra.Common.Decorators
{
    public class ExceptionQueryHandlerDecorator<TReturn> : IAsyncQueryHandler<TReturn>
    {
        private readonly ILogger log;
        private readonly IAsyncQueryHandler<TReturn> decorated;

        public ExceptionQueryHandlerDecorator(IAsyncQueryHandler<TReturn> decorated, ILogger log)
        {
            this.log = log;
            this.decorated = decorated;
        }
        
        public async Task<TReturn> ExecuteAsync()
        {
            var result = default(TReturn);
            try
            {
                result = await decorated.ExecuteAsync();
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"ExceptionCommandHandlerDecorator caught {ex.GetType()} - {ex.Message}");
            }

            return result;
        }
    }
    
    public class ExceptionQueryHandlerDecorator<TParam, TReturn> : IAsyncQueryHandler<TParam, TReturn>
    {
        private readonly ILogger log;
        private readonly IAsyncQueryHandler<TParam, TReturn> decorated;

        public ExceptionQueryHandlerDecorator(IAsyncQueryHandler<TParam, TReturn> decorated, ILogger log)
        {
            this.log = log;
            this.decorated = decorated;
        }
        
        public async Task<TReturn> ExecuteAsync(TParam param)
        {
            var result = default(TReturn);
            try
            {
                result = await decorated.ExecuteAsync(param);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"ExceptionCommandHandlerDecorator caught {ex.GetType()} - {ex.Message}");
            }

            return result;
        }
    }
}