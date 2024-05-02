namespace Electra.Services
{
    public sealed class FeaturesService : FeatureServiceBase
    {
        public FeaturesService(IFeatureStore store, ILogger<FeaturesService> log) : base(store, log)
        {
        }
    }
}