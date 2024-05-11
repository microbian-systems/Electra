namespace Electra.Actors;

public sealed class PingGrain(ILogger<PingGrain> logger) : Grain
{
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OnActivateAsync()");
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        logger.LogInformation("OnDeactivateAsync({Reason})", reason);
        return Task.CompletedTask;
    }

    public ValueTask Ping() => ValueTask.CompletedTask;
}