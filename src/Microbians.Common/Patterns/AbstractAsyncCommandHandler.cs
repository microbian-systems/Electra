using System.Threading.Tasks;
using Microbians.Common.Commands;
using Microsoft.Extensions.Logging;

namespace Microbians.Common.Patterns
{
    public abstract class AbstractAsyncCommandHandler : IAsyncCommand
    {
        private readonly ILogger<AbstractAsyncCommandHandler> log;
        protected AbstractAsyncCommandHandler(ILogger<AbstractAsyncCommandHandler> log) => this.log = log;
        public abstract Task ExecuteAsync();
    }
    
    public abstract class AbstractAsyncCommandHandler<T> : IAsyncCommand<T>
    {
        private readonly ILogger<AbstractAsyncCommandHandler> log;
        protected AbstractAsyncCommandHandler(ILogger<AbstractAsyncCommandHandler> log) => this.log = log;
        public abstract Task ExecuteAsync(T param);
    }
    
    public abstract class AbstractAsyncCommandHandler<T, TReturn> : IAsyncCommand<T, TReturn>
    {
        private readonly ILogger<AbstractAsyncCommandHandler> log;
        protected AbstractAsyncCommandHandler(ILogger<AbstractAsyncCommandHandler> log) => this.log = log;
        public abstract Task<TReturn> ExecuteAsync(T param);
    }
}