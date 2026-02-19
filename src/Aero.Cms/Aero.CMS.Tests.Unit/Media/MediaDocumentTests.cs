using Aero.CMS.Core.Media.Models;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Media;

public class MediaDocumentTests
{
    [Fact]
    public void MediaDocument_Properties_Can_Be_Set_And_Retrieved()
    {
        // Arrange
        var doc = new MediaDocument
        {
            Name = "Test Image",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            MediaType = MediaType.Image,
            StorageKey = "uploads/test.jpg",
            AltText = "Alternative text",
            ParentFolderId = "folder123",
            Width = 800,
            Height = 600
        };

        // Assert
        doc.Name.ShouldBe("Test Image");
        doc.FileName.ShouldBe("test.jpg");
        doc.ContentType.ShouldBe("image/jpeg");
        doc.FileSize.ShouldBe(1024);
        doc.MediaType.ShouldBe(MediaType.Image);
        doc.StorageKey.ShouldBe("uploads/test.jpg");
        doc.AltText.ShouldBe("Alternative text");
        doc.ParentFolderId.ShouldBe("folder123");
        doc.Width.ShouldBe(800);
        doc.Height.ShouldBe(600);
    }

    [Fact]
    public void MediaDocument_Default_Values_Are_Empty()
    {
        // Arrange
        var doc = new MediaDocument();

        // Assert
        doc.Name.ShouldBe(string.Empty);
        doc.FileName.ShouldBe(string.Empty);
        doc.ContentType.ShouldBe(string.Empty);
        doc.FileSize.ShouldBe(0);
        doc.MediaType.ShouldBe(MediaType.Image); // Default is Image? Actually first enum value is Image
        doc.StorageKey.ShouldBe(string.Empty);
        doc.AltText.ShouldBeNull();
        doc.ParentFolderId.ShouldBeNull();
        doc.Width.ShouldBeNull();
        doc.Height.ShouldBeNull();
    }

    [Fact]
    public void MediaType_Enum_Has_Expected_Values()
    {
        // Assert
        MediaType.Image.ShouldBe((MediaType)0);
        MediaType.Video.ShouldBe((MediaType)1);
        MediaType.Document.ShouldBe((MediaType)2);
        MediaType.Audio.ShouldBe((MediaType)3);
        MediaType.Other.ShouldBe((MediaType)4);
    }
}