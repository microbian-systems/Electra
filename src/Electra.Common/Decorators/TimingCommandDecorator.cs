using System.Diagnostics;
using System.Threading.Tasks;
using Electra.Common.Commands;
using Microsoft.Extensions.Logging;

namespace Electra.Common.Decorators;

public class TimingCommandDecorator : IAsyncCommand
{
    private readonly ILogger log;
    readonly IAsyncCommand decorated;

    public TimingCommandDecorator(IAsyncCommand decorated, ILogger log)
    {
        this.log = log;
        this.decorated = decorated;
    }

    public async Task ExecuteAsync()
    {
        log.LogInformation($"entered Timing decorator");
        var sw = new Stopwatch();
        sw.Start();
        await decorated.ExecuteAsync();
        sw.Stop();
        log.LogInformation($"{decorated.GetType()} took {sw.ElapsedMilliseconds} ms");
    }
}

public class TimingCommandDecorator<TCommand> : IAsyncCommand<TCommand>
{
    private readonly ILogger log;
    readonly IAsyncCommand<TCommand> decorated;

    public TimingCommandDecorator(IAsyncCommand<TCommand> decorated, ILogger log)
    {
        this.log = log;
        this.decorated = decorated;
    }

    public async Task ExecuteAsync(TCommand param)
    {
        var sw = new Stopwatch();
        sw.Start();
        await decorated.ExecuteAsync(param);
        sw.Stop();
        log.LogInformation($"{decorated.GetType()} took {sw.ElapsedMilliseconds} ms");
    }
}

public class TimingCommandDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    private readonly ILogger log;
    readonly IAsyncCommand<TCommand, TReturn> decorated;

    public TimingCommandDecorator(IAsyncCommand<TCommand, TReturn> decorated, ILogger log)
    {
        this.log = log;
        this.decorated = decorated;
    }

    public async Task<TReturn> ExecuteAsync(TCommand param)
    {
        var sw = new Stopwatch();
        sw.Start();
        var result = await decorated.ExecuteAsync(param);
        sw.Stop();
        log.LogInformation($"{decorated.GetType()} took {sw.ElapsedMilliseconds} ms");
        return result;
    }
}