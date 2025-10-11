using Electra.Models.Entities;
using Electra.Persistence.EfCore;

namespace Electra.Persistence;

public interface IApiAuthRepository : IGenericEntityFrameworkRepository<ApiAccountModel>
{
    Task<ApiAccountModel?> GetByApiKey(string apiKey);
}

public sealed class ApiAuthRepository(ElectraDbContext context, ILogger<ApiAuthRepository> log)
    : GenericEntityFrameworkRepository<ApiAccountModel>(context, log), IApiAuthRepository
{
    private readonly DbSet<ApiAccountModel> apiAccountsDb = context.ApiAccounts;
    private readonly DbSet<ApiClaimsModel> apiClaimsDb = context.ApiClaims;

    public override Task<IEnumerable<ApiAccountModel>> GetAllAsync()
    {
        var accounts = apiAccountsDb.AsQueryable()
            .Include(a => a.Claims)
            .AsEnumerable();

        return Task.FromResult(accounts);
    }

    public async Task<ApiAccountModel?> GetByKeyAsync(long key)
    {
        var account = await apiAccountsDb
            .Include(x => x.Claims)
            .SingleOrDefaultAsync(x => x.Id == key);

        return account;
    }

    public override async Task<ApiAccountModel> InsertAsync(ApiAccountModel model)
    {
        await apiAccountsDb.AddAsync(model);

        return model;
    }

    public override Task<ApiAccountModel> UpdateAsync(ApiAccountModel model)
    {
        apiAccountsDb.Update(model);

        return Task.FromResult(model);
    }

    public override Task DeleteAsync(ApiAccountModel model)
    {
        apiAccountsDb.Remove(model);

        return Task.CompletedTask;
    }

    public override async Task DeleteAsync(long id)
    {
        var account = await apiAccountsDb
            .Include(x => x.Claims)
            .SingleOrDefaultAsync(x => x.Id == id);
        await DeleteAsync(account!);
    }

    public override Task<IEnumerable<ApiAccountModel>> FindAsync(Expression<Func<ApiAccountModel, bool>> predicate)
    {
        var accounts = apiAccountsDb.Where(predicate)
            .Include(x => x.Claims)
            .AsEnumerable();

        return Task.FromResult(accounts);
    }

    public async Task<int> SaveChangesAsync() 
        => await context.SaveChangesAsync();

    public async Task<ApiAccountModel?> GetByApiKey(string apiKey)
    {
        var model = await apiAccountsDb
            .Include(x => x.Claims)
            .FirstOrDefaultAsync(x => x.ApiKey == apiKey);

        return model;
    }
}