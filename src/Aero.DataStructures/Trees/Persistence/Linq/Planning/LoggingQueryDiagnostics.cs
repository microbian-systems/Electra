using System;
using Aero.DataStructures.Trees.Persistence.Linq.Translation;
using Microsoft.Extensions.Logging;

namespace Aero.DataStructures.Trees.Persistence.Linq.Planning;

public sealed class LoggingQueryDiagnostics : IQueryDiagnostics
{
    private readonly ILogger<LoggingQueryDiagnostics> _logger;

    public LoggingQueryDiagnostics(ILogger<LoggingQueryDiagnostics> logger)
    {
        _logger = logger;
    }

    public void ReportIndexScan(string collectionName, IndexScanSpec spec, bool hasResidual) =>
        _logger.LogDebug(
            "[{Collection}] Index scan on '{Index}' " +
            "(point={IsPoint}, residual={HasResidual})",
            collectionName, spec.Index.Name, spec.IsPoint, hasResidual);

    public void ReportFullScan(string collectionName, object query) =>
        _logger.LogWarning(
            "[{Collection}] Full collection scan — consider adding an index.",
            collectionName);
}
