using Electra.Common.Commands;
using Proto;

namespace Electra.Actors.Proto.Actor;

public class ActorCommandHandlerDecorator<TCommand>(IActorManager<IActor> actorManager, ILogger log)
    : IAsyncCommand<TCommand>
    where TCommand : IActorMessage
{
    // todo - investigate the following url for return async void as I'm doing here
    // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
    public async Task ExecuteAsync(TCommand command)
    {
        log.LogInformation($"entered {nameof(ActorCommandHandlerDecorator<TCommand>)}");
        var cmd = command;
        await actorManager.RequestAsync(command);
    }
}

public class ActorCommandHandlerDecorator<TCommand, TResult>(IActorManager<IActor> actorManager, ILogger log)
    : IAsyncCommand<TCommand, TResult>
    where TCommand : IActorMessage
{
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