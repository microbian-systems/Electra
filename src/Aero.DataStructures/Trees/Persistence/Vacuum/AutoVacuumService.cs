using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aero.DataStructures.Trees.Persistence.Vacuum;

public sealed class AutoVacuumService : BackgroundService
{
    private readonly IVacuumable _tree;
    private readonly AutoVacuumOptions _options;
    private readonly ILogger<AutoVacuumService> _logger;

    public AutoVacuumService(
        IVacuumable tree,
        IOptions<AutoVacuumOptions> options,
        ILogger<AutoVacuumService> logger)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "AutoVacuumService started with check interval {Interval}, threshold {Threshold:P0}",
            _options.CheckInterval,
            _options.FragmentationThreshold);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CheckInterval, ct);

                var fragmentation = await _tree.GetFragmentationAsync(ct);

                if (fragmentation < _options.FragmentationThreshold)
                {
                    _logger.LogDebug(
                        "Fragmentation {Ratio:P0} below threshold {Threshold:P0} — skipping",
                        fragmentation,
                        _options.FragmentationThreshold);
                    continue;
                }

                _logger.LogInformation(
                    "Fragmentation {Ratio:P0} exceeds threshold {Threshold:P0} — vacuuming",
                    fragmentation,
                    _options.FragmentationThreshold);

                var progress = new Progress<VacuumProgress>(p =>
                    _logger.LogDebug(
                        "Vacuum {Processed}/{Total} pages, {Bytes} bytes reclaimed",
                        p.ProcessedPages,
                        p.TotalPages,
                        p.BytesReclaimed));

                await _tree.VacuumAsync(_options.FragmentationThreshold, progress, ct);

                _logger.LogInformation("Vacuum complete");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-vacuum execution");
            }
        }

        _logger.LogInformation("AutoVacuumService stopped");
    }
}
