using Electra.Common.Commands;

namespace Electra.Common.Decorators
{
    public abstract class DecoratorBaseAsync<T> : ICommandAsync<T>
    {
        protected readonly ICommandAsync<T> cmd;
        protected readonly ILogger<DecoratorBaseAsync<T>> log;

        protected DecoratorBaseAsync(ICommandAsync<T> cmd, ILogger<DecoratorBaseAsync<T>> log)
        {
            this.cmd = cmd;
            this.log = log;
        }
        
        public abstract Task ExecuteAsync(T parameter);
    }

    public abstract class DecoratorBase<T, TReturn> : ICommandAsync<T, TReturn> where T : ICommandParameter
    {
        private readonly ICommandAsync<T, TReturn> cmd;
        private readonly ILogger<DecoratorBaseAsync<T>> log;

        protected DecoratorBase(ICommandAsync<T, TReturn> cmd, ILogger<DecoratorBaseAsync<T>> log)
        {
            this.cmd = cmd;
            this.log = log;
        }
        public abstract Task<TReturn> ExecuteAsync(T param);
        
        public virtual async Task<TReturn> ExecuteAsync(Func<T, Task<TReturn>> func, T parameter)
        {
            log.LogInformation($"wrapping {typeof(T)} through the Func<T> decorator");
            var result = await func(parameter);
            log.LogInformation($"successfuly wrapped {typeof(T)}");
            return result;
        }
    }
}