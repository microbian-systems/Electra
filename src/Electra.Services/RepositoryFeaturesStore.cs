using Electra.Persistence.Core;

namespace Electra.Services;


// todo - finish implementing feature store
public class RepositoryFeaturesStore : FeatureStoreBase
{
    private readonly string appName;
    private readonly IGenericRepository<Features.Features> repo;
    private readonly AppSettings settings;

    public RepositoryFeaturesStore(
        IGenericRepository<Features.Features> repo,
        AppSettings settings, ILogger<RepositoryFeaturesStore> log) : base(log)
    {
        this.repo = repo;
        this.settings = settings;
    }

    public override async Task<Features.Features> GetFeatureAsync(string value)
    {
        log.LogInformation($"getting feature: {value}");
        var result = await GetAllFeaturesAsync();
        var feature = result.First(x => string.Equals(x.Feature,
            value, StringComparison.InvariantCultureIgnoreCase));

        return feature;
    }

    public override async Task<List<Features.Features>> GetAllFeaturesAsync()
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
        // log.LogInformation($"getting all features for {AppSettings.AppName}");
        // return await repo.GetAllAsync();
    }

    public override async Task SetFeaturesAsync(Features.Features value)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public override async Task SetFeatureAsync(Features.Features value)
    {
        await Task.CompletedTask;
        log.LogInformation($"setting feature for: {value.ToJson()}");
        throw new NotImplementedException();
        // var features = await GetAllFeaturesAsync();
        //
        // var index = features.FindIndex(x =>
        //     string.Equals(value.Feature, x.Feature, StringComparison.InvariantCultureIgnoreCase));
        //
        // if (index >= 0)
        // {
        //     features[index.Value] = value;
        // }
        // else
        // {
        //     features.Add(value);
        // }
        //
        // await repo.UpsertAsync((Features) features);
    }


    public override async Task DeleteFeatureAsync(string feature)
    {
        await Task.CompletedTask;
        log.LogInformation($"deleting feature {feature}");

        throw new NotImplementedException();
        // var features = await GetAllFeaturesAsync();
        // var item = features?.Featuress?.First(x => 
        //     string.Equals(x.Feature, feature, StringComparison.InvariantCultureIgnoreCase));
        // features?.Featuress?.Remove(item);
        //
        // if (features == null)
        // {
        //     log.LogInformation($"unable to find feature {feature}");
        //     return;
        // }
        //
        // await repo.UpsertAsync((Features) features);
    }

    public override async Task DeleteFeaturesAsync()
    {
        await Task.CompletedTask;
        log.LogWarning($"deleting all features ");

        throw new NotImplementedException();
        // var features = await GetAllFeaturesAsync();
        // await repo.DeleteAsync((Features) features);
    }
}