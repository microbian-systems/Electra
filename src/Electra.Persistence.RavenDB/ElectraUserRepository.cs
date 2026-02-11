using System.Linq.Expressions;
using Electra.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.RavenDB;

public interface IElectraUserRepository
{
    IEnumerable<ElectraUser> GetAll();
    Task<long> CountAsync();
    Task<bool> ExistsAsync(string id);
    Task<IEnumerable<ElectraUser>> GetAllAsync();
    Task<Option<ElectraUser>> GetByIdAsync(string id);
    Task<IEnumerable<ElectraUser>> GetByIdsAsync(IEnumerable<string> ids);
    Option<ElectraUser> FindById(String id);
    IEnumerable<ElectraUser> Find(Expression<Func<ElectraUser, bool>> predicate);
    Task<IEnumerable<ElectraUser>> FindAsync(Expression<Func<ElectraUser, bool>> predicate);
    Task<Option<ElectraUser>> FindByIdAsync(string id);
    Option<ElectraUser> Insert(ElectraUser entity);
    Option<ElectraUser> Update(ElectraUser entity);
    Option<ElectraUser> Upsert(ElectraUser entity);
    bool Delete(String id);
    bool Delete(ElectraUser entity);
    Task<Option<ElectraUser>> InsertAsync(ElectraUser entity);
    Task<Option<ElectraUser>> UpdateAsync(ElectraUser entity);
    Task<Option<ElectraUser>> UpsertAsync(ElectraUser entity);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteAsync(ElectraUser entity);
}

public class ElectraUserRepository : RavenDbRepositoryBase<ElectraUser>, IElectraUserRepository
{
    public ElectraUserRepository(IAsyncDocumentSession session, ILogger<ElectraUserRepository> log) : base(session, log)
    {
        session.Advanced.WaitForIndexesAfterSaveChanges(TimeSpan.FromSeconds(1));
    }
}