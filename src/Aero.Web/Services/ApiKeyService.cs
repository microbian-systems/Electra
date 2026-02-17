using Aero.Common.Web.Extensions;
using Aero.Common.Web.Infrastructure;
using Aero.Common.Web.Jwt;
using Aero.Models;
using Aero.Models.Entities;
using Aero.Persistence;

namespace Aero.Common.Web.Services;

public interface IApiKeyService : IApiService<ApiKeyAuthRequestModel, string> { }

public class ApiKeyService : ApiServiceBase<ApiKeyAuthRequestModel, string>, IApiKeyService
{
    private readonly IApiKeyFactory apiKeyFactory;
    private readonly IClaimsPrincipalFactory claimsFactory;
    private readonly IJwtFactory jwtFactory;
    private readonly JwtOptions jwtOptions;
    //private readonly IAeroUnitOfWork uow;

    public ApiKeyService(
        IApiKeyFactory factory,
        IJwtFactory jwtFactory,
        IClaimsPrincipalFactory claimsFactory,
        //IAeroUnitOfWork uow,
        IOptions<JwtOptions> jwtOptions,
        ILogger<ApiKeyService> log)
        : base(log)
    {
        //this.uow = uow;
        this.apiKeyFactory = factory;
        this.claimsFactory = claimsFactory;
        this.jwtFactory = jwtFactory;
        this.jwtOptions = jwtOptions.Value;
    }

    public override async Task<ApiAccountModel> Register(ApiRegistrationRequest request)
    {
        var principle =
            await claimsFactory.CreateClaimsPrincipal(request.Email);
        var claims = principle.Claims
            .Select(x => new ApiClaimsModel()
            {
                ClaimKey = x.Type,
                ClaimValue = x.Value
            }).ToList();

        var apiKey = claims.First(x => x.ClaimKey == "Id").ClaimValue;
        var account = new ApiAccountModel()
        {
            ApiKey = apiKey,
            Email = request.Email,
            Enabled = true,
            Claims = claims,
            RefreshToken = jwtFactory.GenerateRefreshToken(),
            RefreshTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.RefreshExpiryInMinutes),
            CreateDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
        };

        var model = await Register(account);

        return model;
    }

    // todo - move this out to a facgory or make it private (really belongs in a repository
    [Obsolete("Remove submission of ApiAccountModel directly from service", false)]
    public override async Task<ApiAccountModel> Register(ApiAccountModel model)
    {
        await uow.AuthRepo.InsertAsync(model);
        await uow.SaveChangesAsync();

        return model;
    }

    // todo - change update method signature to use a UpdateAccountRequest view model
    public override async Task<ApiAccountModel> Update(ApiAccountModel model)
    {
        await uow.AuthRepo.UpdateAsync(model);
        await uow.SaveChangesAsync();

        return model;
    }

    public override bool TryGetRefreshToken(RefreshTokenRequest request, out RefreshTokenResponse response)
    {
        var accessToken = request.AccessToken;
        var refreshToken = request.RefreshToken;

        var principal = jwtFactory.GetPrincipalFromToken(accessToken);
        if (principal == null)
        {
            response = new();
            return false;
        }
        
        var claims = principal.Claims.ToList();
        var id = principal.Claims.FirstOrDefault(x => x.Type == "Id")?.Value;
        var user = GetAccountByApiKey(id!).GetAwaiter().GetResult();

        if (user is null || !user.IsRefreshDateValid() || !user.IsRefreshTokenValid(refreshToken))
        {
            response = new();
            return false;
        }

        var newAccessToken = jwtFactory.GenerateAccessToken(claims);
        var newRefreshToken = jwtFactory.GenerateRefreshToken();
        
        response = new RefreshTokenResponse()
        {
            AccessToken = newAccessToken.AccessToken,
            Expiration = newAccessToken.Expiry,
            RefreshToken = newRefreshToken
        };
        
        user.RefreshToken = newRefreshToken;
        uow.SaveChangesAsync().GetAwaiter().GetResult();
        return true;
    }


    public override async Task<bool> Revoke(string apiKey)
    {
        var model = await uow.AuthRepo.GetByApiKey(apiKey);

        if (model == null)
        {
            return false;
        }

        model.Enabled = false;
        await uow.AuthRepo.UpdateAsync(model);

        await uow.SaveChangesAsync();
        return true;
    }

    public override async Task RevokeAll(string email)
    {
        var accounts = await uow.AuthRepo.FindAsync(x => x.Email == email);

        foreach (var account in accounts)
        {
            account.Enabled = false;
            await uow.AuthRepo.UpdateAsync(account);
        }

        await uow.SaveChangesAsync();
    }

    public override async Task<ApiAccountModel?> GetAccountById(string id)
    {
        var account = await uow.AuthRepo.FindByIdAsync(id);

        return account!;
    }

    public override async Task<ApiAccountModel?> GetAccountByApiKey(string key)
    {
        var account = await uow.AuthRepo.GetByApiKey(key);

        return account!;
    }

    public override async Task<List<ApiAccountModel>> GetAccountsByEmail(string email)
    {
        var accounts =
            (await uow.AuthRepo.FindAsync(x => x.Email == email))
            .ToList();

        return accounts;
    }

    public override async Task<ApiAccountModel?> Authenticate(ApiKeyAuthRequestModel model)
    {
        var account = await uow.AuthRepo.GetByApiKey(model.ApiKey);
        return account is not { Enabled: true } ? null! : account;
    }
}