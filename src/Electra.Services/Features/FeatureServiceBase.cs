using System.Collections.Generic;
using System.Threading.Tasks;
using Electra.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Electra.Services.Features;

public abstract class FeatureServiceBase : IFeaturesService
{
    protected readonly ILogger<FeatureServiceBase> log;
    protected readonly IFeatureStore store;

    protected FeatureServiceBase(IFeatureStore store, ILogger<FeatureServiceBase> log)
    {
        this.log = log;
        this.store = store;
    }

    public Features GetFeature(string feature) => GetFeatureAsync(feature).GetAwaiter().GetResult();

    public async Task<Features> GetFeatureAsync(string feature)
    {
        log.LogInformation($"getting feaeture {feature}");
        return await store.GetFeatureAsync(feature);
    }

    public List<Features> GetAllFeatures() => GetAllFeaturesAsync().GetAwaiter().GetResult();

    public async Task<List<Features>> GetAllFeaturesAsync()
    {
        log.LogInformation($"getting all features");
        return await store.GetAllFeaturesAsync();
    }

    public void SetFeature(Features feature) => SetFeatureAsync(feature).GetAwaiter().GetResult();
        
    public async Task SetFeatureAsync(Features feature)
    {
        log.LogInformation($"setting feature {feature.ToJson()}");
        await store.SetFeatureAsync(feature);
    }

    public void SetFeatures(Features features) => SetFeaturesAsync(features).GetAwaiter().GetResult();

    public async Task SetFeaturesAsync(Features features)
    {
        log.LogInformation($"setting all features {features.ToJson()}");
        await store.SetFeaturesAsync(features);
    }

    public void DeleteFeature(string feature) => DeleteFeatureAsync(feature).GetAwaiter().GetResult();

    public async Task DeleteFeatureAsync(string feature)
    {
        log.LogInformation($"deleting feature {feature}");
        await store.DeleteFeatureAsync(feature);
    }

    public void DeleteAllFeatures() => DeleteAllFeaturesAsync().GetAwaiter().GetResult();

    public async Task DeleteAllFeaturesAsync()
    {
        log.LogWarning($"*** warning **** - deleting all features");
        await store.DeleteFeaturesAsync();
    }
}