namespace Aero.Services.Features;

public interface IFeatureStore
{
    Features GetFeature(string value);
    Task<Features> GetFeatureAsync(string value);
    List<Features> GetAllFeatures();
    Task<List<Features>> GetAllFeaturesAsync();
    void SetFeature(Features value);
    Task SetFeatureAsync(Features value);
    void SetFeatures(Features value);
    Task SetFeaturesAsync(Features value);
    void DeleteFeature(string feature);
    Task DeleteFeatureAsync(string feature);
    void DeleteFeatures();
    Task DeleteFeaturesAsync();
}