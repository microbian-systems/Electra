using Aero.Cms.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Aero.Cms.Services
{
    public class BlockRenderer : IBlockRenderer
    {
        public async Task<IHtmlContent> RenderBlockAsync(IHtmlHelper htmlHelper, BlockDocument block, PageRenderContext context)
        {
            // Convention: Block type "Hero" -> Partial view "_Block_Hero"
            // or "Blocks/Hero"
            var partialName = $"Blocks/{block.Type}";
            
            try
            {
                return await htmlHelper.PartialAsync(partialName, block);
            }
            catch (InvalidOperationException)
            {
                // Fallback or error handling for missing partial
                return new HtmlString($"<!-- Missing renderer for block type: {block.Type} -->");
            }
        }
    }
}
