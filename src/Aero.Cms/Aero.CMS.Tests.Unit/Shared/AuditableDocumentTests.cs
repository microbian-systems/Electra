using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Shared;

public class AuditableDocumentTests
{
    private class TestDocument : AuditableDocument { }

    [Fact]
    public void NewInstance_HasNonEmptyId()
    {
        var doc = new TestDocument();
        doc.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(doc.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public void NewInstance_IdMapsToInterfaceGuid()
    {
        var doc = new TestDocument();
        var guidId = ((IEntity<Guid>)doc).Id;
        guidId.ShouldNotBe(Guid.Empty);
        guidId.ToString().ShouldBe(doc.Id);
    }

    [Fact]
    public void NewInstance_CreatedAtIsDefault()
    {
        var doc = new TestDocument();
        doc.CreatedAt.ShouldBe(default);
    }

    [Fact]
    public void NewInstance_UpdatedAtIsNull()
    {
        var doc = new TestDocument();
        doc.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void NewInstance_UpdatedByIsNull()
    {
        var doc = new TestDocument();
        doc.UpdatedBy.ShouldBeNull();
    }

    [Fact]
    public void TwoInstances_HaveDifferentIds()
    {
        var doc1 = new TestDocument();
        var doc2 = new TestDocument();
        
        doc1.Id.ShouldNotBe(doc2.Id);
    }
}
