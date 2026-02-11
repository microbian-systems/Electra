using Electra.Cms.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Electra.Cms.Services
{
    public interface IBlockRenderer
    {
        Task<IHtmlContent> RenderBlockAsync(IHtmlHelper htmlHelper, BlockDocument block, PageRenderContext context);
    }
}
