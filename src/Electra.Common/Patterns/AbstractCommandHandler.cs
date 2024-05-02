using Electra.Common.Commands;
using Microsoft.Extensions.Logging;

namespace Electra.Common.Patterns
{
    public abstract class AbstractCommandHandler : ICommand
    {
        private readonly ILogger log;
        public AbstractCommandHandler(ILogger log) => this.log = log;
        public abstract void Execute();
    }
    
    public abstract class AbstractCommandHandler<T> : ICommand<T>
    {
        private readonly ILogger log;
        public AbstractCommandHandler(ILogger log) => this.log = log;
        public abstract void Execute(T param);
        public void Execute(ICommandParameter param)
        {
            throw new System.NotImplementedException();
        }
    }
    
    public abstract class AbstractCommandHandler<T, TReturn> : ICommand<T, TReturn>
    {
        private readonly ILogger log;
        public AbstractCommandHandler(ILogger log) => this.log = log;
        public abstract TReturn Execute(T param);
    }
}