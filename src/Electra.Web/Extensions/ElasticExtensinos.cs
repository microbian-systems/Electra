using Elasticsearch.Net;
using Electra.Persistence;
using Electra.Persistence.Elastic;
using Nest;

namespace Electra.Common.Web.Extensions;

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