using System;
using Aero.CMS.Core.Shared.Models;
using Aero.CMS.Core.Seo.Models;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Seo;

public class SeoRedirectDocumentTests
{
    [Fact]
    public void NewInstance_ExtendsAuditableDocument()
    {
        var doc = new SeoRedirectDocument();
        doc.ShouldBeAssignableTo<AuditableDocument>();
    }

    [Fact]
    public void NewInstance_HasDefaultValues()
    {
        var doc = new SeoRedirectDocument();
        
        doc.FromSlug.ShouldBeNull();
        doc.ToSlug.ShouldBeNull();
        doc.StatusCode.ShouldBe(301);
        doc.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var doc = new SeoRedirectDocument
        {
            FromSlug = "/old-url",
            ToSlug = "/new-url",
            StatusCode = 302,
            IsActive = false
        };
        
        doc.FromSlug.ShouldBe("/old-url");
        doc.ToSlug.ShouldBe("/new-url");
        doc.StatusCode.ShouldBe(302);
        doc.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void NewInstance_HasNonEmptyId()
    {
        var doc = new SeoRedirectDocument();
        doc.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(doc.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public void TwoInstances_HaveDifferentIds()
    {
        var doc1 = new SeoRedirectDocument();
        var doc2 = new SeoRedirectDocument();
        
        doc1.Id.ShouldNotBe(doc2.Id);
    }
}