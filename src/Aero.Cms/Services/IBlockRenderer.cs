using Aero.Cms.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Aero.Cms.Services
{
    public interface IBlockRenderer
    {
        Task<IHtmlContent> RenderBlockAsync(IHtmlHelper htmlHelper, BlockDocument block, PageRenderContext context);
    }
}
