using Microsoft.Extensions.Logging;

namespace Electra.Cloudflare;

using CloudFlare.Client;
using CloudFlare.Client.Api.Authentication;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Enumerators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

public partial class DnsUpdaterHostedService : BackgroundService, IHostedService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DnsUpdaterHostedService> log;
    private readonly IAuthentication _authentication;
    private readonly TimeSpan _updateInterval;

    public DnsUpdaterHostedService(HttpClient httpClient, ILogger<DnsUpdaterHostedService> log, IConfiguration config)
    {
        this.httpClient = httpClient;
        this.log = log;
        var token = config.GetValue<string>("CloudFlare:ApiToken");
        var email = config.GetValue<string>("CloudFlare:Email");
        var key = config.GetValue<string>("CloudFlare:ApiKey");

        _authentication = !string.IsNullOrEmpty(token)
            ? new ApiTokenAuthentication(token)
            : new ApiKeyAuthentication(email, key);
        _updateInterval = TimeSpan.FromSeconds(config.GetValue("UpdateIntervalSeconds", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await UpdateDnsAsync(cancellationToken);
            log.LogInformation("Finished update process. Waiting '{@UpdateInterval}' for next check", _updateInterval);

            await Task.Delay(_updateInterval, cancellationToken);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("Started DNS updater...");
    }

    public async Task UpdateDnsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new CloudFlareClient(_authentication);
            var externalIpAddress = await GetIpAddressAsync(cancellationToken);
            log.LogDebug("Got ip from external provider: {IP}", externalIpAddress?.ToString());

            if (externalIpAddress == null)
            {
                log.LogError("All external IP providers failed to resolve the IP");
                return;
            }

            var zones = (await client.Zones.GetAsync(cancellationToken: cancellationToken)).Result;
            log.LogDebug("Found the following zones : {@Zones}", zones.Select(x => x.Name));

            foreach (var zone in zones)
            {
                var records = (await client.Zones.DnsRecords.GetAsync(zone.Id,
                    new DnsRecordFilter { Type = DnsRecordType.A }, null, cancellationToken)).Result;
                log.LogDebug("Found the following 'A' records in zone '{Zone}': {@Records}", zone.Name,
                    records.Select(x => x.Name));

                foreach (var record in records)
                {
                    if (record.Type == DnsRecordType.A && record.Content != externalIpAddress.ToString())
                    {
                        var modified = new ModifiedDnsRecord
                        {
                            Type = DnsRecordType.A,
                            Name = record.Name,
                            Content = externalIpAddress.ToString(),
                        };
                        var updateResult =
                            (await client.Zones.DnsRecords.UpdateAsync(zone.Id, record.Id, modified,
                                cancellationToken));

                        if (!updateResult.Success)
                        {
                            log.LogError(
                                "The following errors happened during update of record '{Record}' in zone '{Zone}': {@Error}",
                                record.Name, zone.Name, updateResult.Errors);
                        }
                        else
                        {
                            log.LogInformation(
                                "Successfully updated record '{Record}' ip from '{PreviousIp}' to '{ExternalIpAddress}' in zone '{Zone}'",
                                record.Name, record.Content, externalIpAddress.ToString(), zone.Name);
                        }
                    }
                    else
                    {
                        log.LogDebug(
                            "The IP for record '{Record}' in zone '{Zone}' is already '{ExternalIpAddress}'",
                            record.Name, zone.Name, externalIpAddress.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unexpected exception happened");
        }
    }

    private async Task<IPAddress> GetIpAddressAsync(CancellationToken cancellationToken)
    {
        IPAddress ipAddress = null;
        foreach (var provider in ExternalIpProviders.Providers)
        {
            if (ipAddress != null)
            {
                break;
            }

            var response = await httpClient.GetAsync(provider, cancellationToken);
            if (!response.IsSuccessStatusCode)
                continue;
            log.LogInformation("Successfully got ip from provider {provider}", provider);

            var ip = await response.Content.ReadAsStringAsync(cancellationToken);
            MyRegex().Replace(ip, string.Empty);
            ipAddress = IPAddress.Parse(ip);
        }

        return ipAddress;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [GeneratedRegex(@"\t|\n|\r")]
    private static partial Regex MyRegex();
}