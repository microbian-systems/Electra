namespace Aero.Services.Features;

public abstract class FeatureStoreBase : IFeatureStore
{
    protected readonly ILogger<FeatureStoreBase> log;

    protected FeatureStoreBase(ILogger<FeatureStoreBase> log)
    {
        this.log = log;
    }
        
    public virtual Features GetFeature(string value) => GetFeatureAsync(value).GetAwaiter().GetResult();
        
    public abstract Task<Features> GetFeatureAsync(string value);
        
    public virtual List<Features> GetAllFeatures() => GetAllFeaturesAsync().GetAwaiter().GetResult();

    public abstract Task<List<Features>> GetAllFeaturesAsync();
        
    // public virtual void SetFeature(Features value) => SetFeatureAsync(value).GetAwaiter().GetResult();
    //
    // public abstract Task SetFeatureAsync(Features value);

    public virtual void SetFeatures(Features value) => SetFeaturesAsync(value).GetAwaiter().GetResult();

    public abstract Task SetFeaturesAsync(Features value);
    public void DeleteFeature(string feature) => DeleteFeatureAsync(feature).GetAwaiter().GetResult();
        
    public abstract Task DeleteFeatureAsync(string feature);

    public void DeleteFeatures() => DeleteFeaturesAsync().GetAwaiter().GetResult();

    public abstract Task DeleteFeaturesAsync();

    public virtual void SetFeature(Features value) => SetFeatureAsync(value).GetAwaiter().GetResult();
        
    public abstract Task SetFeatureAsync(Features value);
}