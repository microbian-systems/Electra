using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class LayoutBlocksTests
{
    [Fact]
    public void HeroBlock2_Type_Is_heroBlock2()
    {
        var block = new HeroBlock2();
        block.Type.ShouldBe("heroBlock2");
    }

    [Fact]
    public void HeroBlock2_Title_RoundTrips()
    {
        var block = new HeroBlock2();
        block.Title = "Welcome";
        block.Title.ShouldBe("Welcome");
        block.Properties["title"].ShouldBe("Welcome");
    }

    [Fact]
    public void HeroBlock2_Subtitle_RoundTrips()
    {
        var block = new HeroBlock2();
        block.Subtitle = "Subtitle text";
        block.Subtitle.ShouldBe("Subtitle text");
        block.Properties["subtitle"].ShouldBe("Subtitle text");
    }

    [Fact]
    public void HeroBlock2_CallToActionText_RoundTrips()
    {
        var block = new HeroBlock2();
        block.CallToActionText = "Click me";
        block.CallToActionText.ShouldBe("Click me");
        block.Properties["callToActionText"].ShouldBe("Click me");
    }

    [Fact]
    public void HeroBlock2_CallToActionUrl_RoundTrips()
    {
        var block = new HeroBlock2();
        block.CallToActionUrl = "/action";
        block.CallToActionUrl.ShouldBe("/action");
        block.Properties["callToActionUrl"].ShouldBe("/action");
    }

    [Fact]
    public void HeroBlock2_BackgroundColor_Defaults_To_Slate_50()
    {
        var block = new HeroBlock2();
        block.BackgroundColor.ShouldBe("#f8fafc");
    }

    [Fact]
    public void HeroBlock2_TextColor_Defaults_To_Gray_800()
    {
        var block = new HeroBlock2();
        block.TextColor.ShouldBe("#1f2937");
    }

    [Fact]
    public void OneColumnRowBlock_Implements_ICompositeContentBlock()
    {
        var block = new OneColumnRowBlock();
        block.ShouldBeAssignableTo<ICompositeContentBlock>();
    }

    [Fact]
    public void OneColumnRowBlock_Type_Is_oneColumnRowBlock()
    {
        var block = new OneColumnRowBlock();
        block.Type.ShouldBe("oneColumnRowBlock");
    }

    [Fact]
    public void OneColumnRowBlock_Padding_RoundTrips()
    {
        var block = new OneColumnRowBlock();
        block.Padding = "p-4";
        block.Padding.ShouldBe("p-4");
        block.Properties["padding"].ShouldBe("p-4");
    }

    [Fact]
    public void OneColumnRowBlock_Gap_RoundTrips()
    {
        var block = new OneColumnRowBlock();
        block.Gap = "gap-4";
        block.Gap.ShouldBe("gap-4");
        block.Properties["gap"].ShouldBe("gap-4");
    }

    [Fact]
    public void OneColumnRowBlock_BackgroundColor_RoundTrips()
    {
        var block = new OneColumnRowBlock();
        block.BackgroundColor = "bg-blue-50";
        block.BackgroundColor.ShouldBe("bg-blue-50");
        block.Properties["backgroundColor"].ShouldBe("bg-blue-50");
    }

    [Fact]
    public void TwoColumnRowBlock_Type_Is_twoColumnRowBlock()
    {
        var block = new TwoColumnRowBlock();
        block.Type.ShouldBe("twoColumnRowBlock");
    }

    [Fact]
    public void TwoColumnRowBlock_Column1Width_Defaults_To_50_Percent()
    {
        var block = new TwoColumnRowBlock();
        block.Column1Width.ShouldBe("50%");
    }

    [Fact]
    public void TwoColumnRowBlock_Column2Width_Defaults_To_50_Percent()
    {
        var block = new TwoColumnRowBlock();
        block.Column2Width.ShouldBe("50%");
    }

    [Fact]
    public void TwoColumnRowBlock_Column1Width_RoundTrips()
    {
        var block = new TwoColumnRowBlock();
        block.Column1Width = "60%";
        block.Column1Width.ShouldBe("60%");
        block.Properties["column1Width"].ShouldBe("60%");
    }

    [Fact]
    public void TwoColumnRowBlock_Column2Width_RoundTrips()
    {
        var block = new TwoColumnRowBlock();
        block.Column2Width = "40%";
        block.Column2Width.ShouldBe("40%");
        block.Properties["column2Width"].ShouldBe("40%");
    }

    [Fact]
    public void TwoColumnRowBlock_ResponsiveBreakpoint_Defaults_To_md()
    {
        var block = new TwoColumnRowBlock();
        block.ResponsiveBreakpoint.ShouldBe("md");
    }

    [Fact]
    public void ThreeColumnRowBlock_Type_Is_threeColumnRowBlock()
    {
        var block = new ThreeColumnRowBlock();
        block.Type.ShouldBe("threeColumnRowBlock");
    }

    [Fact]
    public void ThreeColumnRowBlock_EqualColumns_Defaults_To_True()
    {
        var block = new ThreeColumnRowBlock();
        block.EqualColumns.ShouldBeTrue();
    }

    [Fact]
    public void ThreeColumnRowBlock_EqualColumns_RoundTrips()
    {
        var block = new ThreeColumnRowBlock();
        block.EqualColumns = false;
        block.EqualColumns.ShouldBeFalse();
        block.Properties["equalColumns"].ShouldBe(false);
    }

    [Fact]
    public void ThreeColumnRowBlock_ResponsiveBreakpoint_Defaults_To_md()
    {
        var block = new ThreeColumnRowBlock();
        block.ResponsiveBreakpoint.ShouldBe("md");
    }

    [Fact]
    public void FourColumnRowBlock_Type_Is_fourColumnRowBlock()
    {
        var block = new FourColumnRowBlock();
        block.Type.ShouldBe("fourColumnRowBlock");
    }

    [Fact]
    public void FourColumnRowBlock_EqualColumns_Defaults_To_True()
    {
        var block = new FourColumnRowBlock();
        block.EqualColumns.ShouldBeTrue();
    }

    [Fact]
    public void FourColumnRowBlock_ResponsiveBreakpoint_Defaults_To_md()
    {
        var block = new FourColumnRowBlock();
        block.ResponsiveBreakpoint.ShouldBe("md");
    }

    [Fact]
    public void All_Column_Blocks_Have_Empty_AllowedChildTypes_Array()
    {
        var oneColumn = new OneColumnRowBlock();
        var twoColumn = new TwoColumnRowBlock();
        var threeColumn = new ThreeColumnRowBlock();
        var fourColumn = new FourColumnRowBlock();

        oneColumn.AllowedChildTypes.ShouldBeEmpty();
        twoColumn.AllowedChildTypes.ShouldBeEmpty();
        threeColumn.AllowedChildTypes.ShouldBeEmpty();
        fourColumn.AllowedChildTypes.ShouldBeEmpty();
    }

    [Fact]
    public void All_Column_Blocks_Have_AllowNestedComposites_True()
    {
        var oneColumn = new OneColumnRowBlock();
        var twoColumn = new TwoColumnRowBlock();
        var threeColumn = new ThreeColumnRowBlock();
        var fourColumn = new FourColumnRowBlock();

        oneColumn.AllowNestedComposites.ShouldBeTrue();
        twoColumn.AllowNestedComposites.ShouldBeTrue();
        threeColumn.AllowNestedComposites.ShouldBeTrue();
        fourColumn.AllowNestedComposites.ShouldBeTrue();
    }

    [Fact]
    public void All_Column_Blocks_Have_MaxChildren_Null()
    {
        var oneColumn = new OneColumnRowBlock();
        var twoColumn = new TwoColumnRowBlock();
        var threeColumn = new ThreeColumnRowBlock();
        var fourColumn = new FourColumnRowBlock();

        oneColumn.MaxChildren.ShouldBeNull();
        twoColumn.MaxChildren.ShouldBeNull();
        threeColumn.MaxChildren.ShouldBeNull();
        fourColumn.MaxChildren.ShouldBeNull();
    }

    [Fact]
    public void All_Blocks_Have_NonEmpty_Guid_Id()
    {
        var hero = new HeroBlock2();
        var oneColumn = new OneColumnRowBlock();
        var twoColumn = new TwoColumnRowBlock();
        var threeColumn = new ThreeColumnRowBlock();
        var fourColumn = new FourColumnRowBlock();

        hero.Id.ShouldNotBe(Guid.Empty);
        oneColumn.Id.ShouldNotBe(Guid.Empty);
        twoColumn.Id.ShouldNotBe(Guid.Empty);
        threeColumn.Id.ShouldNotBe(Guid.Empty);
        fourColumn.Id.ShouldNotBe(Guid.Empty);
    }
}