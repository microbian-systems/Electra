namespace Electra.Common.Caching.Decorators;

// public sealed class CachingCommandDecorator<T> : DecoratorBaseAsync<T>, ICachingCommandDecorator<T>
// {
//     private readonly ICacheClient cache;
//     //private readonly string prefix = "ace"; // todo - get the cache prefix value from appSettings.json
//     
//     public CachingCommandDecorator(
//         ICacheClient cache, 
//         ICommandAsync<T> cmd, 
//         ILogger<CachingCommandDecorator<T>> log) 
//         : base(cmd, log)
//     {
//         this.cache = cache;
//     }
//
//     public T Execute(string key, ICommandParameter parameter)
//         => ExecuteAsync(key, parameter).GetAwaiter().GetResult();
//
//     public async Task<T> ExecuteAsync(string key, ICommandParameter parameter)
//     {
//         if(string.IsNullOrEmpty(key))
//             key = $"{this.GetType()}"; // todo - return error if not key is provided
//         
//         var typ = cmd.GetType();
//         log.LogInformation($"wrapping {typ} through the caching decorator");
//         var entry = await cache.GetAsync<T>(key);
//
//         if (entry != null)
//             return entry;
//
//         log.LogInformation($"cache hit miss... retrieving value from non-cache");
//         
//         var result = await cmd.ExecuteAsync(parameter);
//         
//         if (result != null)
//             await cache.AddAsync(key, result, new CacheOptions());  // todo - pass this inside the parameter
//
//         log.LogInformation($"successfuly wrapped {typeof(T)}");
//         return result;
//     }
//
//     public override async Task<T> ExecuteAsync(ICommandParameter parameter)
//         => await ExecuteAsync(null, parameter);
//
//     public T Execute(ICommandParameter parameter) 
//         => ExecuteAsync(parameter).GetAwaiter().GetResult();
// }