using Electra.Models;
using Electra.Models.Entities;

namespace Electra.Common.Web.Services;

public interface IApiService<T, TKey> //where T : IAuthRequestModel
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    Task<ApiAccountModel?> GetAccountById(TKey id);
    Task<ApiAccountModel?> GetAccountByApiKey(string key);
    Task<List<ApiAccountModel>> GetAccountsByEmail(string email);
    Task<ApiAccountModel?> Authenticate(T model);
    bool TryGetRefreshToken(RefreshTokenRequest request, out RefreshTokenResponse response);
    Task<ApiAccountModel> Register(ApiRegistrationRequest request);
    Task<ApiAccountModel> Register(ApiAccountModel model);
    Task<ApiAccountModel> Update(ApiAccountModel model);
    Task Revoke(string apiKey);
    Task RevokeAll(string email);
}


public abstract class ApiServiceBase<T, TKey>(ILogger<ApiServiceBase<T, TKey>> log)
    : IApiService<T, TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    protected readonly ILogger<ApiServiceBase<T, TKey>> log = log;

    public abstract Task<ApiAccountModel?> GetAccountById(TKey id);
    public abstract Task<ApiAccountModel?> GetAccountByApiKey(string key);
    public abstract Task<List<ApiAccountModel>> GetAccountsByEmail(string email);
    public abstract Task<ApiAccountModel?> Authenticate(T model);
    public abstract bool TryGetRefreshToken(RefreshTokenRequest request, out RefreshTokenResponse response);
    public abstract Task<ApiAccountModel> Register(ApiRegistrationRequest request);
    public abstract Task<ApiAccountModel> Register(ApiAccountModel model);
    public abstract Task<ApiAccountModel> Update(ApiAccountModel model);
    public abstract Task Revoke(string apiKey);
    public abstract Task RevokeAll(string email);
}