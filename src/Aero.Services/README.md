# Aero.Services

Business logic services, feature toggles, and user management for the Aero framework.

## Overview

`Aero.Services` contains the core business logic of the application, including domain services, feature flag management, user profile services, and cross-cutting business operations.

## Key Components

### Feature Store

```csharp
public interface IFeatureStore
{
    Task<bool> IsEnabledAsync(string featureName);
    Task<bool> IsEnabledForUserAsync(string featureName, string userId);
    Task<FeatureFlag> GetFeatureAsync(string featureName);
    Task<IEnumerable<FeatureFlag>> GetAllFeaturesAsync();
}

public class RepositoryFeaturesStore : FeatureStoreBase
{
    private readonly IGenericRepository<FeatureFlag> _repository;
    private readonly IFusionCache _cache;

    public RepositoryFeaturesStore(
        IGenericRepository<FeatureFlag> repository,
        IFusionCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public override async Task<bool> IsEnabledAsync(string featureName)
    {
        var feature = await GetFeatureAsync(featureName);
        return feature?.Enabled ?? false;
    }

    public override async Task<bool> IsEnabledForUserAsync(string featureName, string userId)
    {
        var feature = await GetFeatureAsync(featureName);
        
        if (feature == null) return false;
        if (feature.EnabledForAll) return true;
        
        return feature.EnabledForUsers?.Contains(userId) ?? false;
    }

    public override async Task<FeatureFlag> GetFeatureAsync(string featureName)
    {
        return await _cache.GetOrSetAsync(
            $"feature:{featureName}",
            async _ => await _repository.FindOneAsync(f => f.Name == featureName),
            TimeSpan.FromMinutes(5));
    }
}

public class FeatureFlag : Entity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
    public bool EnabledForAll { get; set; }
    public List<string> EnabledForUsers { get; set; } = new();
    public List<string> EnabledForRoles { get; set; } = new();
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int? PercentageRollout { get; set; }
}
```

### User Profile Service

```csharp
public interface IUserProfileService
{
    Task<UserProfile> GetProfileAsync(string userId);
    Task<UserProfile> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task UploadAvatarAsync(string userId, Stream imageStream);
    Task<IEnumerable<ActivityLog>> GetActivityHistoryAsync(string userId, int count);
}

public class UserProfileService : IUserProfileService
{
    private readonly IGenericRepository<UserProfile> _profileRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<UserProfileService> _logger;

    public async Task<UserProfile> GetProfileAsync(string userId)
    {
        var profile = await _profileRepository.FindByIdAsync(userId);
        
        if (profile == null)
        {
            // Create default profile
            profile = new UserProfile
            {
                Id = userId,
                CreatedOn = DateTimeOffset.UtcNow
            };
            await _profileRepository.InsertAsync(profile);
        }

        return profile;
    }

    public async Task<UserProfile> UpdateProfileAsync(
        string userId, 
        UpdateProfileRequest request)
    {
        var profile = await GetProfileAsync(userId);
        
        profile.FirstName = request.FirstName ?? profile.FirstName;
        profile.LastName = request.LastName ?? profile.LastName;
        profile.Bio = request.Bio ?? profile.Bio;
        profile.TimeZone = request.TimeZone ?? profile.TimeZone;
        profile.ModifiedOn = DateTimeOffset.UtcNow;

        return await _profileRepository.UpdateAsync(profile);
    }

    public async Task UploadAvatarAsync(string userId, Stream imageStream)
    {
        // Process and resize image
        using var resizedImage = await ImageProcessor.ResizeAsync(imageStream, 400, 400);
        
        // Upload to blob storage
        var avatarUrl = await _blobStorage.UploadAsync(
            $"avatars/{userId}.jpg",
            resizedImage,
            "image/jpeg");

        // Update profile
        var profile = await GetProfileAsync(userId);
        profile.AvatarUrl = avatarUrl;
        await _profileRepository.UpdateAsync(profile);
    }
}
```

### Feature Toggle Usage

```csharp
public class ProductService
{
    private readonly IFeatureStore _featureStore;
    private readonly IGenericRepository<Product> _repository;

    public async Task<ProductDto> GetProductAsync(string id)
    {
        var product = await _repository.FindByIdAsync(id);
        if (product == null) return null;

        var dto = product.MapTo<ProductDto>();

        // Feature-gated functionality
        if (await _featureStore.IsEnabledAsync("ProductReviews"))
        {
            dto.Reviews = await GetReviewsAsync(id);
        }

        if (await _featureStore.IsEnabledAsync("ProductRecommendations"))
        {
            dto.Recommendations = await GetRecommendationsAsync(id);
        }

        return dto;
    }
}
```

## Configuration

```csharp
builder.Services.AddAeroServices();

public static class ServiceExtensions
{
    public static IServiceCollection AddAeroServices(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IFeatureStore, RepositoryFeaturesStore>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
```

## Related Packages

- `Aero.Core` - Entity definitions
- `Aero.Persistence` - Data access
- `Aero.Caching` - Feature flag caching
