using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Electra.Actors;

public interface IPongGrain : IGrainWithGuidKey
{
    [Alias("Pong")]
    Task Pong(Message message);
}

public class PongGrain(ILogger<PongGrain> log) : AeroGrain(log), IPongGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("PongGrain activated.");
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        log.LogInformation("PongGrain deactivated.");
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task Pong(Message message)
    {
        log.LogInformation($"Ping received: {message.content}");

        var id = NewId.NextSequentialGuid();
        // Create a response message
        var responseMessage = new Message(id, $"pong! ping received: {message.content}");

        // Log the response
        log.LogInformation(responseMessage.content);

        return Task.CompletedTask;
    }
}