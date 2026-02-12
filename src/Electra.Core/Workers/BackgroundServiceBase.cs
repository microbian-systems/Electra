using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Electra.Core.Workers;

public abstract class BackgroundServiceBase(
    IServiceProvider sp,
    ILogger<BackgroundServiceBase> log,
    IConfiguration config)
    : BackgroundService
{

    protected abstract override Task ExecuteAsync(CancellationToken stoppingToken);
}