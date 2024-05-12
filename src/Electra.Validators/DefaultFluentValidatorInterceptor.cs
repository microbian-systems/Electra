namespace Electra.Validators
{
    // todo - fix fluent validation IValidatorInterceptor impl
//     public class DefaultFluentValidatorInterceptor : IValidatorInterceptor
//     {
//         private readonly IMemoryCache cache; // todo - consider using cacheManager or Foundatio cache
//         private readonly ILogger<DefaultFluentValidatorInterceptor> log;
//
//         public DefaultFluentValidatorInterceptor(IMemoryCache cache, ILogger<DefaultFluentValidatorInterceptor> log)
//         {
//             this.cache = cache;
//             this.log = log;
//         }
//         protected string RequestId { get; set; }
//         public virtual IValidationContext BeforeMvcValidation(ControllerContext controllerContext, IValidationContext validationContext)
//         {
//             RequestId = controllerContext.HttpContext.TraceIdentifier;
//             return validationContext;
//         }
//
//         public virtual ValidationResult AfterMvcValidation(ControllerContext controllerContext, IValidationContext validationContext, ValidationResult result)
//         {
//             if(!result.IsValid)
//                 cache.Set(RequestId, result, TimeSpan.FromMinutes(1));
//             return result;
//         }
//     }
}