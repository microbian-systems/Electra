using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Aero.TickerQ.RavenDB.Infrastructure;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;

namespace Aero.TickerQ.RavenDB.DependencyInjection;

public static class ServiceExtension
{
    public static TickerOptionsBuilder<TTimeTicker, TCronTicker> AddRavenDbOperationalStore<TTimeTicker, TCronTicker>(
        this TickerOptionsBuilder<TTimeTicker, TCronTicker> tickerConfiguration, 
        Action<TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker>> ravenDbConfiguration = null)
        where TTimeTicker : TimeTickerEntity<TTimeTicker>, new()
        where TCronTicker : CronTickerEntity, new()
    {
        var ravenDbOptionBuilder = new TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker>();

        ravenDbConfiguration?.Invoke(ravenDbOptionBuilder);
            
        if (string.IsNullOrWhiteSpace(ravenDbOptionBuilder.DatabaseName)) 
            throw new ArgumentException("Database name must be specified", nameof(ravenDbOptionBuilder.DatabaseName));
            
        if (ravenDbOptionBuilder.Urls == null || ravenDbOptionBuilder.Urls.Length == 0)
            throw new ArgumentException("At least one RavenDB URL must be specified", nameof(ravenDbOptionBuilder.Urls));
            
        tickerConfiguration.ExternalProviderConfigServiceAction += (services) 
            => services.AddSingleton(_ => ravenDbOptionBuilder);
            
        tickerConfiguration.ExternalProviderConfigServiceAction += ravenDbOptionBuilder.ConfigureServices;
            
        UseApplicationService(tickerConfiguration, ravenDbOptionBuilder);
            
        return tickerConfiguration;
    }
        
    private static void UseApplicationService<TTimeTicker, TCronTicker>(
        TickerOptionsBuilder<TTimeTicker, TCronTicker> tickerConfiguration, 
        TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker> options)
        where TTimeTicker : TimeTickerEntity<TTimeTicker>, new()
        where TCronTicker : CronTickerEntity, new()
    {
        tickerConfiguration.UseExternalProviderApplication((serviceProvider) =>
        {
            var internalTickerManager = serviceProvider.GetRequiredService<IInternalTickerManager>();
            var hostLifetime = serviceProvider.GetService<IHostApplicationLifetime>();
            var schedulerOptions = serviceProvider.GetService<SchedulerOptionsBuilder>();
            var hostScheduler = serviceProvider.GetService<ITickerQHostScheduler>();

            hostLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    await internalTickerManager.ReleaseDeadNodeResources(schedulerOptions.NodeIdentifier);
                                        
                    if (hostScheduler != null && hostScheduler.IsRunning)
                    {
                        hostScheduler.Restart();
                    }
                });
            });
        });
    }
}

public class TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker>
    where TTimeTicker : TimeTickerEntity<TTimeTicker>, new()
    where TCronTicker : CronTickerEntity, new()
{
    public string[] Urls { get; private set; }
    public string DatabaseName { get; private set; }
    public string CertificatePath { get; private set; }
    public string CertificatePassword { get; private set; }
    public Action<DocumentStore> ConfigureDocumentStore { get; private set; }

    public TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker> WithUrls(params string[] urls)
    {
        Urls = urls;
        return this;
    }

    public TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker> WithDatabase(string databaseName)
    {
        DatabaseName = databaseName;
        return this;
    }

    public TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker> WithCertificate(string certificatePath, string password = null)
    {
        CertificatePath = certificatePath;
        CertificatePassword = password;
        return this;
    }

    public TickerQRavenDbOptionBuilder<TTimeTicker, TCronTicker> ConfigureStore(Action<DocumentStore> configureAction)
    {
        ConfigureDocumentStore = configureAction;
        return this;
    }

    internal void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDocumentStore>(sp =>
        {
            var store = new DocumentStore
            {
                Urls = Urls,
                Database = DatabaseName
            };

            if (!string.IsNullOrWhiteSpace(CertificatePath))
            {
                store.Certificate = string.IsNullOrWhiteSpace(CertificatePassword)
                    ? System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(CertificatePath)
                    : System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(CertificatePath, CertificatePassword);
            }

            ConfigureDocumentStore?.Invoke(store);

            store.Initialize();

            return store;
        });

        services.AddSingleton<ITickerPersistenceProvider<TTimeTicker, TCronTicker>, 
            TickerRavenDbPersistenceProvider<TTimeTicker, TCronTicker>>();
    }
}
