using System;
using Electra.Cms.Blocks;
using Electra.Cms.Indexes;
using Electra.Cms.Middleware;
using Electra.Cms.Options;
using Electra.Cms.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;

namespace Electra.Cms
{
    public static class Extensions
    {
        public static IServiceCollection AddElectraCms(this IServiceCollection services, Action<CmsOptions>? configureOptions = null)
        {
            var options = new CmsOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

            services.AddSingleton<IBlockRegistry, BlockRegistry>();
            services.AddScoped<ISiteResolver, SiteResolver>();
            services.AddScoped<IPageRouter, PageRouter>();
            services.AddScoped<IBlockRenderer, BlockRenderer>();
            services.AddScoped<ICmsContext, CmsContext>();
            
            return services;
        }

        public static IApplicationBuilder UseElectraCms(this IApplicationBuilder app)
        {
            app.UseMiddleware<SiteResolutionMiddleware>();
            app.UseMiddleware<PageRoutingMiddleware>();
            app.UseMiddleware<CmsOutputCachingMiddleware>();
            
            return app;
        }

        public static void EnsureCmsIndexes(this IDocumentStore store)
        {
            new Pages_BySiteAndUrl().Execute(store);
            new Sites_ByHostname().Execute(store);
        }
    }
}
