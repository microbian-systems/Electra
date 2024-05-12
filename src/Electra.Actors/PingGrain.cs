using MassTransit;

namespace Electra.Actors;

public interface IPingGrain
{
    Task Ping();
}

public class GrainBase(ILogger<GrainBase> log) : Grain
{
    protected readonly ILogger<GrainBase> log = log;
}

public class PingGrain(ILogger<PingGrain> log, IGrainFactory grainFactory) : Grain, IPingGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("PingGrain activated.");
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        log.LogInformation("PingGrain deactivated.");
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task Ping()
    {
        log.LogInformation("Ping received.");
        var id = NewId.NextSequentialGuid();
        // Create a message to send
        var message = new Message(id, "ping!" );

        // Get a reference to the PongGrain using GrainFactory
        var pongGrain = grainFactory.GetGrain<IPongGrain>(Guid.NewGuid());

        // Send the message to PongGrain
        await pongGrain.Pong(message);

        log.LogInformation("Ping sent to PongGrain.");
    }
}