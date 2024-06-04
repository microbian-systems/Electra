using Electra.Common.Commands;

namespace Electra.Common.Decorators
{
    public class CpuBoundCommandHandlerDecorator<TCommand> : IAsyncCommand<TCommand> 
    {
        private readonly ILogger log;
        private readonly Func<IAsyncCommand<TCommand>> decorateeFactory;

        public CpuBoundCommandHandlerDecorator(Func<IAsyncCommand<TCommand>> decorateeFactory, ILogger log) {
            this.decorateeFactory = decorateeFactory;
            this.log = log;
        }

        // todo - investigate the following url for return async void as I'm doing here
        // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
        public async Task ExecuteAsync(TCommand command) => await Task.Run(() =>
        {
            log.LogInformation($"entered {nameof(CpuBoundCommandHandlerDecorator<TCommand>)}");
            var cmd = command; 
            // execute on new thread & create new handler in this thread.
            var handler = decorateeFactory.Invoke();
            handler.ExecuteAsync(cmd);
        });
    }
}