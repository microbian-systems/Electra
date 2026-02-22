using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Aero.CMS.Core.Content.ContentFinders;
using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Search;
using Aero.CMS.Core.Content.Search.Extractors;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Data;
using Aero.CMS.Core.Extensions;
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
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Integration.Infrastructure;

public class CompositionRootTests : RavenTestBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _mockEnvironment;

    public CompositionRootTests()
    {
        // Build configuration that points to the embedded RavenDB
        var store = Store; // This initializes the embedded store
        var configValues = new Dictionary<string, string?>
        {
            ["Aero:RavenDb:Urls:0"] = store.Urls[0],
            ["Aero:RavenDb:Database"] = "Aero_CMS_Test",
            ["Aero:RavenDb:EnableRevisions"] = "false"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _mockEnvironment = Substitute.For<IWebHostEnvironment>();
    }

    [Fact]
    public void AddAeroCmsCore_RegistersAllServices_ResolvesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton(_mockEnvironment);
        services.AddAeroCmsCore(_configuration);

        // Replace IDocumentStore registration with the embedded store to avoid duplicate initialization
        services.AddSingleton<IDocumentStore>(_ => Store);

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert: Resolve each service (list from ServiceExtensions)
        // Infrastructure
        serviceProvider.GetRequiredService<IDocumentStore>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ISystemClock>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IKeyVaultService>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IBlockRegistry>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<PluginLoader>().ShouldNotBeNull();

        // Repositories (scoped)
        using (var scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IContentRepository>().ShouldNotBeNull();
            scope.ServiceProvider.GetRequiredService<IContentTypeRepository>().ShouldNotBeNull();
            scope.ServiceProvider.GetRequiredService<ISeoRedirectRepository>().ShouldNotBeNull();
        }

        // Save hooks
        serviceProvider.GetRequiredService<ContentSearchIndexerHook>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IBeforeSaveHook<ContentDocument>>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<SaveHookPipeline<ContentDocument>>().ShouldNotBeNull();

        // Search
        serviceProvider.GetRequiredService<IBlockTreeTextExtractor>().ShouldNotBeNull();

        // Content services
        using (var scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IPublishingWorkflow>().ShouldNotBeNull();
            scope.ServiceProvider.GetRequiredService<ContentFinderPipeline>().ShouldNotBeNull();
            scope.ServiceProvider.GetRequiredService<IContentFinder>().ShouldNotBeNull();
        }

        // Markdown
        serviceProvider.GetRequiredService<MarkdownRendererService>().ShouldNotBeNull();
        using (var scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<MarkdownImportService>().ShouldNotBeNull();
        }

        // Rich text
        serviceProvider.GetRequiredService<IRichTextEditor>().ShouldNotBeNull();

        // Media
        serviceProvider.GetRequiredService<IMediaProvider>().ShouldNotBeNull();

        // Identity
        using (var scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IBanService>().ShouldNotBeNull();
        }
    }

    [Fact]
    public void AddAeroCmsCore_RegistersExpectedCollectionCounts()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton(_mockEnvironment);
        services.AddAeroCmsCore(_configuration);
        services.AddSingleton<IDocumentStore>(_ => Store);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var seoChecks = serviceProvider.GetServices<ISeoCheck>();
        var blockExtractors = serviceProvider.GetServices<IBlockTextExtractor>();
        var contentFinders = serviceProvider.GetServices<IContentFinder>();

        // Assert
        seoChecks.ShouldNotBeNull();
        seoChecks.Count().ShouldBe(4);
        blockExtractors.ShouldNotBeNull();
        blockExtractors.Count().ShouldBe(5);
        contentFinders.ShouldNotBeNull();
        contentFinders.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddAeroCmsCore_RegistersSaveHookPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton(_mockEnvironment);
        services.AddAeroCmsCore(_configuration);
        services.AddSingleton<IDocumentStore>(_ => Store);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var pipeline = serviceProvider.GetRequiredService<SaveHookPipeline<ContentDocument>>();

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    public void AddAeroCmsCore_RegistersOneBeforeSaveHookForContentDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton(_mockEnvironment);
        services.AddAeroCmsCore(_configuration);
        services.AddSingleton<IDocumentStore>(_ => Store);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hooks = serviceProvider.GetServices<IBeforeSaveHook<ContentDocument>>();

        // Assert
        hooks.ShouldNotBeNull();
        hooks.ShouldHaveSingleItem();
    }
}