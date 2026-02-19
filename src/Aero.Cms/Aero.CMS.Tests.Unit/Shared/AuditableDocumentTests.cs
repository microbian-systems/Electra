using Aero.CMS.Core.Shared.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Shared;

public class AuditableDocumentTests
{
    private class TestDocument : AuditableDocument { }

    [Fact]
    public void NewInstance_HasNonEmptyGuidId()
    {
        var doc = new TestDocument();
        doc.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewInstance_HasCreatedAtApproximatelyUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var doc = new TestDocument();
        var after = DateTime.UtcNow.AddSeconds(1);
        
        doc.CreatedAt.ShouldBeInRange(before, after);
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
