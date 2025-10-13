using Electra.Caching.Decorators;

namespace Electra.Services;

public class CachedRepositoryFeatureStore : RepositoryFeaturesStore
{
    public CachedRepositoryFeatureStore(ICachingRepositoryDecorator<Features.Features> db, 
        AppSettings settings, ILogger<RepositoryFeaturesStore> log) : base(db, settings, log)
    {
    }
}