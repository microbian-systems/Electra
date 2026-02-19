using Aero.CMS.Core.Content.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class ContentDocumentTests
{
    [Fact]
    public void NewContentDocument_Has_Status_Draft()
    {
        var doc = new ContentDocument();
        doc.Status.ShouldBe(PublishingStatus.Draft);
    }

    [Fact]
    public void NewContentDocument_Has_Empty_Blocks_List()
    {
        var doc = new ContentDocument();
        doc.Blocks.ShouldNotBeNull();
        doc.Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void NewContentDocument_Has_Null_PublishedAt()
    {
        var doc = new ContentDocument();
        doc.PublishedAt.ShouldBeNull();
    }

    [Fact]
    public void NewContentDocument_Has_Null_ParentId()
    {
        var doc = new ContentDocument();
        doc.ParentId.ShouldBeNull();
    }
}
