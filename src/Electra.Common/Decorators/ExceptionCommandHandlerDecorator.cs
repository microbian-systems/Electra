using System;
using System.Threading.Tasks;
using Electra.Common.Commands;
using Microsoft.Extensions.Logging;

namespace Electra.Common.Decorators;

public class ExceptionCommandHandlerDecorator : IAsyncCommand
{
    private readonly ILogger log;
    private readonly IAsyncCommand decorated;

    public ExceptionCommandHandlerDecorator(IAsyncCommand decorated, ILogger log)
    {
        this.log = log;
        this.decorated = decorated;
    }
        
    public async Task ExecuteAsync()
    {
        try
        {
            await decorated.ExecuteAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"ExceptionCommandHandlerDecorator caught {ex.GetType()} - {ex.Message}");
        }
    }
}
    
public class ExceptionCommandHandlerDecorator<TCommand> : IAsyncCommand<TCommand>
{
    private readonly ILogger log;
    private readonly IAsyncCommand<TCommand> decorated;

    public ExceptionCommandHandlerDecorator(IAsyncCommand<TCommand> decorated, ILogger log)
    {
        this.log = log;
        this.decorated = decorated;
    }
        
    public async Task ExecuteAsync(TCommand param)
    {
        try
        {
            log.LogInformation($"executing ExceptionHandlerDecorator");
            await decorated.ExecuteAsync(param);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"ExceptionCommandHandlerDecorator caught {ex.GetType()} - {ex.Message}");
        }
    }
}
    
public class ExceptionCommandHandlerDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    private readonly ILogger log;
    private readonly IAsyncCommand<TCommand, TReturn> decorated;

    public ExceptionCommandHandlerDecorator(IAsyncCommand<TCommand, TReturn> decorated, ILogger log)
    {
        this.log = log;
        this.decorated = decorated;
    }
        
    public async Task<TReturn> ExecuteAsync(TCommand param)
    {
        var result = default(TReturn);
        try
        {
            log.LogInformation($"executing ExceptionHandlerDecorator");
            result = await decorated.ExecuteAsync(param);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"ExceptionCommandHandlerDecorator caught {ex.GetType()} - {ex.Message}");
        }

        return result;
    }
}