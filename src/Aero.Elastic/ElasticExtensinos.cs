using Aero.Core.Extensions;
using Elasticsearch.Net;
using Aero.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Aero.Persistence.Elastic;

public static class ElasticExtensions
{
    public static IServiceCollection ConfigureElasticsearch(this IServiceCollection services)
    {
        services.AddScoped<IElasticClient>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<ElasticClient>>();
            var config = sp.GetRequiredService<IOptions<AppSettings>>();
            var settings = config.Value;

            log.LogInformation($"configuring elastic search client");
            log.LogInformation($"elastic urls: {settings.ElasticsearchUrls.ToJson()}");
            var pool = new SniffingConnectionPool(settings.ElasticsearchUrls.Select(uri => new Uri(uri)));
            var client = new ElasticClient(new ConnectionSettings(pool).DefaultIndex("defaultIndex"));
            return client;
        });

        services.AddScoped(typeof(IElasticsearchRepository<>),
            typeof(ElasticsearchRepository<>));

        return services;
    }
}