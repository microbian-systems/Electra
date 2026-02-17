using System;
using System.Threading.Tasks;
using Aero.Common.Commands;
using Microsoft.Extensions.Logging;
using Polly;

namespace Aero.Common.Decorators;

public class RetryCommandHandlerDecorator<TCommand> : IAsyncCommand<TCommand>
{
    private readonly ILogger<RetryCommandHandlerDecorator<TCommand>> log;
    private readonly IAsyncCommand<TCommand> handler;

    public RetryCommandHandlerDecorator(IAsyncCommand<TCommand> handler, ILogger<RetryCommandHandlerDecorator<TCommand>> log) {
        this.log = log;
        this.handler = handler;
    }

    // todo - investigate the following url for return async void as I'm doing here
    // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
    public async Task ExecuteAsync(TCommand command) 
    {
        log.LogInformation($"entered {nameof(RetryCommandHandlerDecorator<TCommand>)}");
        const int maxRetryAttempts = 5;
        var pauseBetweenFailures = TimeSpan.FromSeconds(2);

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(maxRetryAttempts, i => pauseBetweenFailures);

        await retryPolicy.ExecuteAsync(async () =>
        {
            await handler.ExecuteAsync(command);
        });
    }
}
    
    
public class RetryCommandHandlerDecorator<TCommand, TResult> : IAsyncCommand<TCommand, TResult>
{
    private readonly ILogger log;
    private readonly IAsyncCommand<TCommand, TResult> handler;

    public RetryCommandHandlerDecorator(IAsyncCommand<TCommand, TResult> handler, ILogger log) {
        this.log = log;
        this.handler = handler;
    }
        
    // todo - investigate the following url for return async void as I'm doing here
    // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
    public async Task<TResult> ExecuteAsync(TCommand command) 
    {
        log.LogInformation($"entered {nameof(RetryCommandHandlerDecorator<TCommand>)}");
        const int maxRetryAttempts = 5;
        var pauseBetweenFailures = TimeSpan.FromSeconds(2);

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(maxRetryAttempts, i => pauseBetweenFailures);

        var results = default(TResult);
        await retryPolicy.ExecuteAsync(async () =>
        {
            results = await handler.ExecuteAsync(command);
        });
        return results;
    }
}