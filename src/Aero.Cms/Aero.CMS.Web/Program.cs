using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Extensions;
using Aero.CMS.Core.Plugins.Interfaces;
using Aero.CMS.Core.Site.Data;
using Aero.CMS.Core.Site.Services;
using Aero.CMS.Web.Components;
using Aero.CMS.Web.Components.Blocks;
using Aero.CMS.Components.Admin.PageSection;
using Aero.CMS.Core.Plugins;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    ;

builder.Services.AddAeroCmsCore(builder.Configuration);
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<SectionService>();
builder.Services.AddHostedService<SiteBootstrapService>();

builder.Services.AddSingleton(sp =>
{
    var registry = sp.GetRequiredService<IBlockRegistry>();
    registry.Register<RichTextBlock, RichTextBlockView>();
    registry.Register<ImageBlock, ImageBlockView>();
    registry.Register<HeroBlock, HeroBlockView>();
    registry.Register<QuoteBlock, QuoteBlockView>();
    registry.Register<MarkdownBlock, MarkdownBlockView>();
    registry.Register<HtmlBlock, HtmlBlockView>();
    registry.Register<SectionBlock, SectionBlockView>();
    registry.Register<ColumnBlock, ColumnBlockView>();
    return registry;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(PageList).Assembly);

app.Run();
