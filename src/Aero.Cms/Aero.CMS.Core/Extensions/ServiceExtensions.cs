using System.Collections.Generic;
using Aero.CMS.Core.Content.ContentFinders;
using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Search;
using Aero.CMS.Core.Content.Search.Extractors;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Data;
using Aero.CMS.Core.Media.Interfaces;
using Aero.CMS.Core.Media.Providers;
using Aero.CMS.Core.Membership.Services;
using Aero.CMS.Core.Plugins;
using Aero.CMS.Core.Plugins.Interfaces;
using Aero.CMS.Core.Seo.Checks;
using Aero.CMS.Core.Seo.Data;
using Aero.CMS.Core.Seo.Interfaces;
using Aero.CMS.Core.Settings;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace Aero.CMS.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAeroCmsCore(this IServiceCollection services, IConfiguration configuration)
    {
        var ravenDbSettings = configuration.GetSection("Aero:RavenDb").Get<RavenDbSettings>() 
                             ?? new RavenDbSettings();

        services.AddSingleton<IDocumentStore>(_ => DocumentStoreFactory.Create(ravenDbSettings));
        
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IKeyVaultService, EnvironmentKeyVaultService>();
        services.AddSingleton<IBlockRegistry, BlockRegistry>();
        services.AddSingleton<PluginLoader>();

        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
        services.AddScoped<ISeoRedirectRepository, SeoRedirectRepository>();

        services.AddScoped<ContentSearchIndexerHook>();
        services.AddScoped<IBeforeSaveHook<ContentDocument>>(sp => 
            sp.GetRequiredService<ContentSearchIndexerHook>());

        services.AddSingleton<SaveHookPipeline<ContentDocument>>(sp =>
            new SaveHookPipeline<ContentDocument>(
                sp.GetServices<IBeforeSaveHook<ContentDocument>>(),
                sp.GetServices<IAfterSaveHook<ContentDocument>>()));

        services.AddSingleton<IBlockTreeTextExtractor, BlockTreeTextExtractor>();
        services.AddSingleton<IBlockTextExtractor, RichTextBlockExtractor>();
        services.AddSingleton<IBlockTextExtractor, MarkdownBlockExtractor>();
        services.AddSingleton<IBlockTextExtractor, ImageBlockExtractor>();
        services.AddSingleton<IBlockTextExtractor, HeroBlockExtractor>();
        services.AddSingleton<IBlockTextExtractor, QuoteBlockExtractor>();

        services.AddScoped<IPublishingWorkflow, PublishingWorkflow>();
        services.AddScoped<ContentFinderPipeline>();
        services.AddScoped<IContentFinder, DefaultContentFinder>();

        services.AddSingleton<MarkdownRendererService>();
        services.AddScoped<MarkdownImportService>();

        services.AddSingleton<IRichTextEditor, NullRichTextEditor>();

        services.AddSingleton<IMediaProvider, DiskStorageProvider>();

        services.AddSingleton<ISeoCheck, PageTitleSeoCheck>();
        services.AddSingleton<ISeoCheck, MetaDescriptionSeoCheck>();
        services.AddSingleton<ISeoCheck, HeadingOneSeoCheck>();
        services.AddSingleton<ISeoCheck, WordCountSeoCheck>();

        services.AddScoped<IBanService, BanService>();

        return services;
    }
}
