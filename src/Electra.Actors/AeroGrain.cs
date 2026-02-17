using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Electra.Actors;

/// <summary>
/// Base grain class for actors
/// </summary>
/// <param name="log">ILogger<T/> instance for logging</param>
public abstract class AeroGrain(ILogger<AeroGrain> log) : Grain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (reason.ReasonCode == DeactivationReasonCode.ShuttingDown)
        {
            log.LogInformation("Grain deactivated ... shutting down");
            MigrateOnIdle();
        }
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}