using System.Net.Sockets;
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
    private readonly IAuthentication authentication;
    private readonly TimeSpan updateInterval;

    public DnsUpdaterHostedService(HttpClient httpClient, ILogger<DnsUpdaterHostedService> log, IConfiguration config)
    {
        this.httpClient = httpClient;
        this.log = log;
        var token = config.GetValue<string>("CloudFlare:ApiToken");
        var email = config.GetValue<string>("CloudFlare:Email");
        var key = config.GetValue<string>("CloudFlare:ApiKey");

        this.authentication = !string.IsNullOrEmpty(token)
            ? new ApiTokenAuthentication(token)
            : new ApiKeyAuthentication(email, key);
        this.updateInterval = TimeSpan.FromSeconds(config.GetValue("UpdateIntervalSeconds", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await UpdateDnsAsync(cancellationToken);
            log.LogInformation("Finished update process. Waiting '{@UpdateInterval}' for next check", updateInterval);

            await Task.Delay(updateInterval, cancellationToken);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("Started DNS updater...");
        await base.StartAsync(cancellationToken);
    }

    public async Task UpdateDnsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new CloudFlareClient(authentication);
            var externalIpAddress = await GetIpAddressAsync(cancellationToken);
            log.LogDebug("Got ip from external provider: {IP}", externalIpAddress?.ToString());

            if (externalIpAddress == null)
            {
                log.LogError("All external IP providers failed to resolve the IP");
                return;
            }

            var zones = (await client.Zones.GetAsync(cancellationToken: cancellationToken))
                .Result
                .Where(x => x.Status == ZoneStatus.Active && x.Name.Contains("triviatitans"))
                .ToList();
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
                            Type = record.Type,
                            Name = record.Name,
                            Content = externalIpAddress.ToString(),
                            Comment = $"last updated: {DateTimeOffset.Now}",
                            Ttl = record.Ttl,
                            Proxied = record.Proxied
                        };
                        var updateResult =
                            (await client.Zones.DnsRecords.UpdateAsync(zone.Id, record.Id, modified,
                                cancellationToken));

                        if (!updateResult.Success)
                        {
                            var deleteResult = (await client.Zones.DnsRecords.DeleteAsync(zone.Id, record.Id, cancellationToken));
                            if (!deleteResult.Success)
                            {
                                log.LogError(
                                    "The following errors happened during delete of record '{Record}' in zone '{Zone}': {@Error}",
                                    record.Name, zone.Name, deleteResult.Errors);
                            }
                            var newRecord = new NewDnsRecord
                            {
                                Type = record.Type,
                                Name = modified.Name,
                                Content = externalIpAddress.ToString(),
                                Comment = $"last updated: {DateTimeOffset.Now}",
                                Ttl = modified.Ttl,
                                Proxied = modified.Proxied
                            };
                            updateResult = (await client.Zones.DnsRecords.AddAsync(zone.Id, newRecord,
                                cancellationToken));
                        }

                        if (!updateResult.Success)
                        {
                            log.LogError(
                                "The following errors happened during update of record '{Record}' in zone '{Zone}': {@Error}",
                                record.Name, zone.Name, updateResult.Errors);
                            return;
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
        foreach (var provider in ExternalIpProviders.Providers)
        {
            var response = await httpClient.GetAsync(provider, cancellationToken);
            if (!response.IsSuccessStatusCode)
                continue;

            var ip = await response.Content.ReadAsStringAsync(cancellationToken);
            log.LogInformation("Successfully got ip from provider {provider} - {ip}", provider, ip);
            
            ip = MyRegex().Replace(ip ?? string.Empty, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ip))
                continue;
            IPAddress ipAddress = null;
            if (IPAddress.TryParse(ip, out var parsedIpAddress))
            {
                ipAddress = parsedIpAddress;
            }
            
            if (ipAddress?.AddressFamily == AddressFamily.InterNetworkV6)
                ipAddress = null;
            if (ipAddress != null)
            {
                log.LogInformation("Successfully parsed ip v4 from provider {provider} - {ip}", provider, ipAddress);
                return ipAddress;
            };
            
        }
        log.LogError("Failed to get ip from all providers");
        return null;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("Stopping DNS updater...");
        await base.StopAsync(cancellationToken);
    }

    [GeneratedRegex(@"\t|\n|\r")]
    private static partial Regex MyRegex();
}