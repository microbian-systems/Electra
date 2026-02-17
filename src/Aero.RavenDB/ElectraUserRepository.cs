using System.Linq.Expressions;
using Aero.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Aero.RavenDB;

public interface IAeroUserRepository
{
    IEnumerable<AeroUser> GetAll();
    Task<long> CountAsync();
    Task<bool> ExistsAsync(string id);
    Task<IEnumerable<AeroUser>> GetAllAsync();
    Task<Option<AeroUser>> GetByIdAsync(string id);
    Task<IEnumerable<AeroUser>> GetByIdsAsync(IEnumerable<string> ids);
    Option<AeroUser> FindById(String id);
    IEnumerable<AeroUser> Find(Expression<Func<AeroUser, bool>> predicate);
    Task<IEnumerable<AeroUser>> FindAsync(Expression<Func<AeroUser, bool>> predicate);
    Task<Option<AeroUser>> FindByIdAsync(string id);
    Option<AeroUser> Insert(AeroUser entity);
    Option<AeroUser> Update(AeroUser entity);
    Option<AeroUser> Upsert(AeroUser entity);
    bool Delete(String id);
    bool Delete(AeroUser entity);
    Task<Option<AeroUser>> InsertAsync(AeroUser entity);
    Task<Option<AeroUser>> UpdateAsync(AeroUser entity);
    Task<Option<AeroUser>> UpsertAsync(AeroUser entity);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteAsync(AeroUser entity);
}

public class AeroUserRepository : RavenDbRepositoryBase<AeroUser>, IAeroUserRepository
{
    public AeroUserRepository(IAsyncDocumentSession session, ILogger<AeroUserRepository> log) : base(session, log)
    {
        session.Advanced.WaitForIndexesAfterSaveChanges(TimeSpan.FromSeconds(1));
    }

    public override async Task<IEnumerable<AeroUser>> GetByIdsAsync(IEnumerable<string> ids)
    {
        throw new NotImplementedException();
    }

    public override async Task<IEnumerable<AeroUser>> FindAsync(Expression<Func<AeroUser, bool>> predicate)
    {
        throw new NotImplementedException();
    }
}