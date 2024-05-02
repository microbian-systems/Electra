using System.Collections.Generic;
using System.Threading.Tasks;
using Electra.Services.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Common.Web.Controllers
{
    [ApiController]
    public abstract class FeaturesController : AppXApiBaseController
    {
        private readonly IFeaturesService service;
        private readonly HttpContext ctx;

        protected FeaturesController(IFeaturesService service, IHttpContextAccessor accessor, ILogger<FeaturesController> log) : base(log)
        {
            this.service = service;
            this.ctx = accessor.HttpContext;
        }
        
        [HttpGet("get/{feature}")]
        public async Task<ActionResult<Features>> SetFeature([FromRoute] string feature)
        {
            if (string.IsNullOrEmpty(feature))
                return BadRequest();
            
            var result = await service.GetFeatureAsync(feature);

            if (result == null)
                return NoContent();
            
            return Ok(result);
        }

        [HttpGet("get_features")]
        public async Task<ActionResult<Features>> GetAceFeatureToggles()
        {
            var features = await service.GetAllFeaturesAsync();
            return Ok(features);
        }
        
        [HttpGet("get_all_features")]
        public async Task<ActionResult<List<Features>>> GetAceFeatureTogglesList()
        {
            var features = await service.GetAllFeaturesAsync();
            return Ok(features);
        }
        
        [HttpPost("set")]
        public async Task<IActionResult> SetFeature([FromBody] Features feature)
        {
            await service.SetFeatureAsync(feature);
            return Ok();
        }

        [HttpPut("update")]
        public async Task<ActionResult> UpdateFeature([FromBody] Features feature)
        {
            await service.SetFeatureAsync(feature);
            return Ok();
        }        
        
        [HttpPut("update_list")]
        public async Task<IActionResult> UpdateFeatures([FromBody] List<Features> model)
        {
            var features = new Features()
            {
                ModifiedBy = ctx?.User?.Identity?.Name,
            };
            
            await service.SetFeaturesAsync(features);
            return Ok();
        }
        
        [HttpDelete("delete/{feature}")]
        public async Task<IActionResult> DeleteFeature([FromRoute] string feature)
        {
            await service.DeleteFeatureAsync(feature);
            return Ok();
        }
        
        [HttpDelete("delete_feature_toggle")]
        public async Task<IActionResult> DeleteFeature()
        {
            await service.DeleteAllFeaturesAsync();
            return Ok();
        }
    }
}