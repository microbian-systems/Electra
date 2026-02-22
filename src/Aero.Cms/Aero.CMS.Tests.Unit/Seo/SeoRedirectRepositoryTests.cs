using System.Threading.Tasks;
using Aero.CMS.Core.Seo.Data;
using Aero.CMS.Core.Seo.Models;
using Aero.CMS.Core.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Seo;

public class SeoRedirectRepositoryTests
{
    private readonly IDocumentStore _store;
    private readonly IAsyncDocumentSession _session;
    private readonly ISeoRedirectRepository _repository;
    private readonly ISystemClock _clock;
    private readonly ILogger<SeoRedirectRepository> _log;

    public SeoRedirectRepositoryTests()
    {
        _log = Substitute.For<ILogger<SeoRedirectRepository>>();
        _store = Substitute.For<IDocumentStore>();
        _session = Substitute.For<IAsyncDocumentSession>();
        _clock = Substitute.For<ISystemClock>();
        _store.OpenAsyncSession().Returns(_session);
        
        _repository = new SeoRedirectRepository(_store, _clock, _log);
    }

    [Fact]
    public void Constructor_InitializesWithDocumentStore()
    {
        _repository.ShouldNotBeNull();
    }

    [Fact]
    public async Task FindByFromSlugAsync_ReturnsNull_WhenSlugIsNull()
    {
        var result = await _repository.FindByFromSlugAsync(null);
        
        result.ShouldBeNull();
        _session.DidNotReceive().Query<SeoRedirectDocument>();
    }

    [Fact]
    public async Task FindByFromSlugAsync_ReturnsNull_WhenSlugIsEmpty()
    {
        var result = await _repository.FindByFromSlugAsync("");
        
        result.ShouldBeNull();
        _session.DidNotReceive().Query<SeoRedirectDocument>();
    }

    [Fact]
    public async Task FindByFromSlugAsync_ReturnsNull_WhenSlugIsWhitespace()
    {
        var result = await _repository.FindByFromSlugAsync("   ");
        
        result.ShouldBeNull();
        _session.DidNotReceive().Query<SeoRedirectDocument>();
    }


}