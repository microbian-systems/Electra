using Aero.CMS.Core.Content.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class ContentTypeDocumentTests
{
    [Fact]
    public void NewContentTypeDocument_Has_Empty_Properties_List()
    {
        var doc = new ContentTypeDocument();
        doc.Properties.ShouldNotBeNull();
        doc.Properties.ShouldBeEmpty();
    }

    [Fact]
    public void NewContentTypeDocument_RequiresApproval_Is_False_By_Default()
    {
        var doc = new ContentTypeDocument();
        doc.RequiresApproval.ShouldBeFalse();
    }

    [Fact]
    public void NewContentTypeProperty_Has_NonEmpty_Guid_Id()
    {
        var prop = new ContentTypeProperty();
        prop.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewContentTypeDocument_Has_Default_Icon()
    {
        var doc = new ContentTypeDocument();
        doc.Icon.ShouldBe("document");
    }
}
