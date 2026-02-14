using Raven.Client.Documents.Session;
using ZauberCMS.Core.Content.Models;
using ZauberCMS.Core.Data.Interfaces;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Shared.Models;

namespace ZauberCMS.Web.SeedData;

/// <summary>
/// Seeds default content types and sample pages for the ZauberCMS.Web site
/// </summary>
public class DefaultContentSeeder : ISeedData
{
    public void Initialise(IDocumentSession db)
    {
        // Check if we already have content types
        var existingContentTypes = db.Query<ContentType>().ToList();
        if (existingContentTypes.Any())
        {
            // Content types already exist, don't re-seed
            return;
        }

        // Create Content Types
        var websiteContentType = CreateWebsiteContentType();
        var homePageContentType = CreateHomePageContentType();
        var blogContentType = CreateBlogContentType();
        var blogPostContentType = CreateBlogPostContentType();
        var textPageContentType = CreateTextPageContentType();
        var contactPageContentType = CreateContactPageContentType();

        // Create Element Types (for block lists)
        var richTextEditorElementType = CreateRichTextEditorElementType();
        var quoteElementType = CreateQuoteElementType();
        var imageElementType = CreateImageElementType();
        var faqElementType = CreateFaqElementType();
        var faqItemElementType = CreateFaqItemElementType();

        // Store all content types
        db.Store(websiteContentType);
        db.Store(homePageContentType);
        db.Store(blogContentType);
        db.Store(blogPostContentType);
        db.Store(textPageContentType);
        db.Store(contactPageContentType);
        db.Store(richTextEditorElementType);
        db.Store(quoteElementType);
        db.Store(imageElementType);
        db.Store(faqElementType);
        db.Store(faqItemElementType);

        // Create root Website content
        var websiteContent = CreateWebsiteContent(websiteContentType.Id);
        db.Store(websiteContent);

        // Create Home Page
        var homePage = CreateHomePage(homePageContentType.Id, websiteContent.Id);
        db.Store(homePage);

        // Create Blog Page
        var blogPage = CreateBlogPage(blogContentType.Id, websiteContent.Id);
        db.Store(blogPage);

        // Create About Us Page
        var aboutPage = CreateAboutPage(textPageContentType.Id, websiteContent.Id);
        db.Store(aboutPage);

        // Create Contact Page
        var contactPage = CreateContactPage(contactPageContentType.Id, websiteContent.Id);
        db.Store(contactPage);

        // Create sample blog posts
        var blogPost1 = CreateSampleBlogPost(blogPostContentType.Id, blogPage.Id, "Welcome to ZauberCMS", "zauber-cms-welcome");
        var blogPost2 = CreateSampleBlogPost(blogPostContentType.Id, blogPage.Id, "Getting Started with Content Management", "getting-started");
        db.Store(blogPost1);
        db.Store(blogPost2);

        db.SaveChanges();
    }

    //#region Content Types

    private ContentType CreateWebsiteContentType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Website",
            Alias = "Website",
            Description = "Root website configuration",
            Icon = "web",
            AllowAtRoot = true,
            IsElementType = false,
            AvailableContentViews = [],
            ContentProperties = new List<PropertyType>(),
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateHomePageContentType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Home Page",
            Alias = "HomePage",
            Description = "Home page content type",
            Icon = "home",
            AllowAtRoot = true,
            IsElementType = false,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.Pages.HomeView" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Heading", "heading", "Text", 1),
                CreatePropertyType("SubHeading", "subHeading", "Text", 2),
                CreatePropertyType("HeaderImage", "headerImage", "MediaPicker", 3),
                CreatePropertyType("Content", "content", "BlockList", 4)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateBlogContentType()
    {
        var type =  new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Blog",
            Alias = "Blog",
            Description = "Blog listing page",
            Icon = "article",
            AllowAtRoot = true,
            IsElementType = false,
            EnableListView = true,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.Pages.BlogView" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Heading", "heading", "Text", 1),
                CreatePropertyType("SubHeading", "subHeading", "Text", 2),
                CreatePropertyType("HeaderImage", "headerImage", "MediaPicker", 3),
                CreatePropertyType("Content", "content", "BlockList", 4),
                CreatePropertyType("AmountPerPage", "amountPerPage", "Numeric", 5)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateBlogPostContentType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Blog Post",
            Alias = "BlogPost",
            Description = "Individual blog post",
            Icon = "post_add",
            AllowAtRoot = false,
            IsElementType = false,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.Pages.BlogPageView" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Heading", "heading", "Text", 1),
                CreatePropertyType("SubHeading", "subHeading", "Text", 2),
                CreatePropertyType("HeaderImage", "headerImage", "MediaPicker", 3),
                CreatePropertyType("Content", "content", "BlockList", 4)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateTextPageContentType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Text Page",
            Alias = "TextPage",
            Description = "Generic text page",
            Icon = "description",
            AllowAtRoot = true,
            IsElementType = false,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.Pages.TextPageView" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Heading", "heading", "Text", 1),
                CreatePropertyType("SubHeading", "subHeading", "Text", 2),
                CreatePropertyType("HeaderImage", "headerImage", "MediaPicker", 3),
                CreatePropertyType("Content", "content", "BlockList", 4)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateContactPageContentType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Contact Page",
            Alias = "ContactPage",
            Description = "Contact page with form",
            Icon = "contact_mail",
            AllowAtRoot = true,
            IsElementType = false,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.Pages.ContactView" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Heading", "heading", "Text", 1),
                CreatePropertyType("SubHeading", "subHeading", "Text", 2),
                CreatePropertyType("HeaderImage", "headerImage", "MediaPicker", 3),
                CreatePropertyType("Content", "content", "BlockList", 4),
                CreatePropertyType("ContactForm", "contactForm", "ContentPicker", 5)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    //#endregion

    //#region Element Types

    private ContentType CreateRichTextEditorElementType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Rich Text Editor",
            Alias = "RichTextEditor",
            Description = "Rich text content block",
            Icon = "edit_note",
            IsElementType = true,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.ContentBlocks.RTEBlock" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Content", "content", "RichTextEditor", 1)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateQuoteElementType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Quote",
            Alias = "Quote",
            Description = "Quote or citation block",
            Icon = "format_quote",
            IsElementType = true,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.ContentBlocks.QuoteBlock" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Quote Text", "quoteText", "TextArea", 1),
                CreatePropertyType("Author", "author", "Text", 2)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return type;
    }

    private ContentType CreateImageElementType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "Image",
            Alias = "Image",
            Description = "Image block",
            Icon = "image",
            IsElementType = true,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.ContentBlocks.ImageBlock" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Image", "image", "MediaPicker", 1),
                CreatePropertyType("Caption", "caption", "Text", 2)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return  type;
    }

    private ContentType CreateFaqElementType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "FAQ",
            Alias = "FAQ",
            Description = "FAQ accordion block",
            Icon = "help",
            IsElementType = true,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.ContentBlocks.FAQBlock" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Title", "title", "Text", 1),
                CreatePropertyType("FAQ Items", "faqItems", "BlockList", 2)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return  type;
    }

    private ContentType CreateFaqItemElementType()
    {
        var type = new ContentType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = "FAQ Item",
            Alias = "FAQItem",
            Description = "Individual FAQ item",
            Icon = "help_outline",
            IsElementType = true,
            AvailableContentViews = new List<string> { "ZauberCMS.Web.ContentBlocks.FAQItem" },
            ContentProperties = new List<PropertyType>
            {
                CreatePropertyType("Question", "question", "Text", 1),
                CreatePropertyType("Answer", "answer", "RichTextEditor", 2)
            },
            Tabs = new List<Tab> { new() { Id = "8282a912-408f-49dd-ab84-b19600d55aef", IsSystemTab = true, SortOrder = 100, Name = "System" } }
        };
        return  type;
    }

    //#endregion

    //#region Content

    private Content CreateWebsiteContent(string contentTypeId)
    {
        var id = Guid.NewGuid().NewSequentialGuid().ToString();
        var content = new Content
        {
            Id = id,
            Name = "Website",
            ContentTypeId = contentTypeId,
            ContentTypeAlias = "Website",
            IsRootContent = true,
            Published = true,
            Path = new List<string> { id },
            SortOrder = 0,
            ViewComponent = "",
            PropertyData = new List<ContentPropertyValue>()
        };
        
        return content;
    }

    private Content CreateHomePage(string contentTypeId, string parentId)
    {
        var id = Guid.NewGuid().NewSequentialGuid().ToString();
        var content = new Content
        {
            Id = id,
            Name = "Home",
            ContentTypeId = contentTypeId,
            ContentTypeAlias = "HomePage",
            IsRootContent = true,
            Published = true,
            ParentId = parentId,
            Path = new List<string> { parentId, id },
            SortOrder = 1,
            ViewComponent = "ZauberCMS.Web.Pages.HomeView"
        };
        
        content.PropertyData = new List<ContentPropertyValue>
        {
            CreatePropertyValue(id, "heading", "Welcome to ZauberCMS"),
            CreatePropertyValue(id, "subHeading", "A clean, modern CMS built with .NET"),
            CreatePropertyValue(id, "headerImage", ""),
            CreatePropertyValue(id, "content", "[]")
        };
        
        return content;
    }

    private Content CreateBlogPage(string contentTypeId, string parentId)
    {
        var id = Guid.NewGuid().NewSequentialGuid().ToString();
        var content = new Content
        {
            Id = id,
            Name = "Blog",
            ContentTypeId = contentTypeId,
            ContentTypeAlias = "Blog",
            IsRootContent = true,
            Published = true,
            ParentId = parentId,
            Path = new List<string> { parentId, id },
            SortOrder = 2,
            ViewComponent = "ZauberCMS.Web.Pages.BlogView"
        };
        
        content.PropertyData = new List<ContentPropertyValue>
        {
            CreatePropertyValue(id, "heading", "Blog"),
            CreatePropertyValue(id, "subHeading", "Latest news and updates"),
            CreatePropertyValue(id, "headerImage", ""),
            CreatePropertyValue(id, "content", "[]"),
            CreatePropertyValue(id, "amountPerPage", "10")
        };
        
        return content;
    }

    private Content CreateAboutPage(string contentTypeId, string parentId)
    {
        var id = Guid.NewGuid().NewSequentialGuid().ToString();
        var content = new Content
        {
            Id = id,
            Name = "About Us",
            ContentTypeId = contentTypeId,
            ContentTypeAlias = "TextPage",
            IsRootContent = true,
            Published = true,
            ParentId = parentId,
            Path = new List<string> { parentId, id },
            SortOrder = 3,
            ViewComponent = "ZauberCMS.Web.Pages.TextPageView"
        };
        
        content.PropertyData = new List<ContentPropertyValue>
        {
            CreatePropertyValue(id, "heading", "About Us"),
            CreatePropertyValue(id, "subHeading", "Learn more about our story"),
            CreatePropertyValue(id, "headerImage", ""),
            CreatePropertyValue(id, "content", "[]")
        };
        
        return content;
    }

    private Content CreateContactPage(string contentTypeId, string parentId)
    {
        var id = Guid.NewGuid().NewSequentialGuid().ToString();
        var content = new Content
        {
            Id = id,
            Name = "Contact",
            ContentTypeId = contentTypeId,
            ContentTypeAlias = "ContactPage",
            IsRootContent = true,
            Published = true,
            ParentId = parentId,
            Path = new List<string> { parentId, id },
            SortOrder = 4,
            ViewComponent = "ZauberCMS.Web.Pages.ContactView"
        };
        
        content.PropertyData = new List<ContentPropertyValue>
        {
            CreatePropertyValue(id, "heading", "Contact Us"),
            CreatePropertyValue(id, "subHeading", "Get in touch with us"),
            CreatePropertyValue(id, "headerImage", ""),
            CreatePropertyValue(id, "content", "[]"),
            CreatePropertyValue(id, "contactForm", "")
        };
        
        return content;
    }

    private Content CreateSampleBlogPost(string contentTypeId, string parentId, string title, string slug)
    {
        var id = Guid.NewGuid().NewSequentialGuid().ToString();
        var content = new Content
        {
            Id = id,
            Name = title,
            ContentTypeId = contentTypeId,
            ContentTypeAlias = "BlogPost",
            IsRootContent = false,
            Published = true,
            ParentId = parentId,
            Path = new List<string> { parentId, id },
            SortOrder = 0,
            ViewComponent = "ZauberCMS.Web.Pages.BlogPageView"
        };
        
        content.PropertyData = new List<ContentPropertyValue>
        {
            CreatePropertyValue(id, "heading", title),
            CreatePropertyValue(id, "subHeading", "This is a sample blog post"),
            CreatePropertyValue(id, "headerImage", ""),
            CreatePropertyValue(id, "content", "[]")
        };
        
        return content;
    }

    //#endregion

    //#region Helper Methods

    private PropertyType CreatePropertyType(string name, string alias, string component, int sortOrder)
    {
        var type = new PropertyType
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            Name = name,
            Alias = alias,
            Component = component,
            ComponentAlias = component.ToLowerInvariant(),
            SortOrder = sortOrder,
            TabId = "8282a912-408f-49dd-ab84-b19600d55aef",
            TabAlias = "System"
        };
        return type;
    }

    private ContentPropertyValue CreatePropertyValue(string contentId, string alias, string value)
    {
        var prop = new ContentPropertyValue
        {
            Id = Guid.NewGuid().NewSequentialGuid().ToString(),
            ContentId = contentId,
            Alias = alias,
            Value = value,
            ContentTypePropertyId = Guid.NewGuid().NewSequentialGuid().ToString()
        };
        return prop;
    }

    //#endregion
}
