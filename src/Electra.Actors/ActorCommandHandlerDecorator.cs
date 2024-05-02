using System.Threading.Tasks;
using Electra.Common.Commands;
using Microsoft.Extensions.Logging;
using Proto;

namespace Electra.Actors
{
    public class ActorCommandHandlerDecorator<TCommand> : IAsyncCommand<TCommand> where TCommand : IActorMessage
    {
        private readonly ILogger log;
        private readonly IActorManager<IActor> actorManager;

        public ActorCommandHandlerDecorator(IActorManager<IActor> actorManager, ILogger log) {
            this.log = log;
            this.actorManager = actorManager;
        }

        // todo - investigate the following url for return async void as I'm doing here
        // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
        public async Task ExecuteAsync(TCommand command) 
        {
            log.LogInformation($"entered {nameof(ActorCommandHandlerDecorator<TCommand>)}");
            var cmd = command;
            await actorManager.RequestAsync(command);
        }
    }
    
    public class ActorCommandHandlerDecorator<TCommand, TResult> : IAsyncCommand<TCommand, TResult> 
        where TCommand : IActorMessage
    {
        private readonly ILogger log;
        private readonly IActorManager<IActor> actorManager;

        public ActorCommandHandlerDecorator(IActorManager<IActor> actorManager, ILogger log) {
            this.log = log;
            this.actorManager = actorManager;
        }

        // todo - investigate the following url for return async void as I'm doing here
        // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
        public async Task<TResult> ExecuteAsync(TCommand command) 
        {
            log.LogInformation($"entered {nameof(ActorCommandHandlerDecorator<TCommand>)}");
            var cmd = command;
            var results = await actorManager.RequestAsync<TCommand, TResult>(command);
            return results;
        }
    }
}