using System.Collections.Generic;
using System.Threading.Tasks;

namespace Electra.Services.Features;

public interface IFeaturesService
{
    Features GetFeature(string feature);
    Task<Features> GetFeatureAsync(string feature);
    List<Features> GetAllFeatures();
    Task<List<Features>> GetAllFeaturesAsync();
    void SetFeature(Features feature);
    Task SetFeatureAsync(Features feature);
    void SetFeatures(Features features);
    Task SetFeaturesAsync(Features features);
    void DeleteFeature(string feature);
    Task DeleteFeatureAsync(string feature);
    void DeleteAllFeatures();
    Task DeleteAllFeaturesAsync();
}