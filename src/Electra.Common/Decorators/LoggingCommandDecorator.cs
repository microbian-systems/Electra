using System.Threading.Tasks;
using Electra.Common.Commands;
using Microsoft.Extensions.Logging;

namespace Electra.Common.Decorators
{
    public class LoggingCommandDecorator : IAsyncCommand
    {
        private readonly ILogger log;
        private readonly IAsyncCommand decorated;

        public LoggingCommandDecorator(IAsyncCommand decorated, ILogger log)
        {
            this.log = log;
            this.decorated = decorated;
        }

        public async Task ExecuteAsync()
        {
            var type = decorated.GetType();
            log.LogInformation($"starting Execute on {type}");
            await decorated.ExecuteAsync();
            log.LogInformation($"finished Execute() on {type}");
        }
    }
    
    public class LoggingCommandDecorator<TCommand> : IAsyncCommand<TCommand>
    {
        private readonly ILogger log;
        private readonly IAsyncCommand<TCommand> decorated;

        public LoggingCommandDecorator(IAsyncCommand<TCommand> decorated, ILogger log)
        {
            this.log = log;
            this.decorated = decorated;
        }

        public async Task ExecuteAsync(TCommand param)
        {
            var type = decorated.GetType();
            log.LogInformation($"starting Execute on {type}");
            await decorated.ExecuteAsync(param);
            log.LogInformation($"finished Execute() on {type}");
        }
    }
    
    public class LoggingCommandDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
    {
        private readonly ILogger log;
        private readonly IAsyncCommand<TCommand, TReturn> decorated;

        public LoggingCommandDecorator(IAsyncCommand<TCommand, TReturn> decorated, ILogger log)
        {
            this.log = log;
            this.decorated = decorated;
        }

        public async Task<TReturn> ExecuteAsync(TCommand param)
        {
            var type = decorated.GetType();
            log.LogInformation($"starting Execute on {type}");
            var result = await decorated.ExecuteAsync(param);
            log.LogInformation($"finished Execute() on {type}");
            return result;
        }
    }
}