using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using Aero.CMS.Core.Site.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Site;

public class SiteDocumentTests
{
    [Fact]
    public void NewInstance_HasIsDefaultTrue()
    {
        var site = new SiteDocument();
        site.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void NewInstance_HasDefaultLayoutPublicLayout()
    {
        var site = new SiteDocument();
        site.DefaultLayout.ShouldBe("PublicLayout");
    }

    [Fact]
    public void NewInstance_HasNonEmptyId()
    {
        var site = new SiteDocument();
        site.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(site.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public void NewInstance_IdMapsToInterfaceGuid()
    {
        var site = new SiteDocument();
        var guidId = ((IEntity<Guid>)site).Id;
        guidId.ShouldNotBe(Guid.Empty);
        guidId.ToString().ShouldBe(site.Id);
    }

    [Fact]
    public void TwoInstances_HaveDifferentIds()
    {
        var site1 = new SiteDocument();
        var site2 = new SiteDocument();
        site1.Id.ShouldNotBe(site2.Id);
    }
}
