namespace Electra.Persistence;

public interface IGraphRepository<TEntity, TKey> : IWriteRepository<TEntity, TKey> 
    where TEntity : class , IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    IEnumerable<TEntity> Get(int limit, int skip);
    IEnumerable<TEntity> Get(string label);
    IEnumerable<TEntity> Get(string label, int limit, int skip);
    IEnumerable<string> GetLabels(TKey id);
    bool AddLabel(TKey id, string label);
    bool DeleteLabel(TKey id, string label);
    IEnumerable<TOut> GetRelated<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>;
    IEnumerable<TOut> GetRelated<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey> where TRelation : class;
    int GetRelatedCount<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>;
    int GetRelatedCount<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey> where TRelation : class;
    bool AddRelationShip<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>;
    bool AddRelationShip<TOut, TRelation>(TKey inboundId, TKey outboundId, string relationship, TRelation relation) where TOut : class, IEntity<TKey> where TRelation : class;
    bool DeleteRelationShip<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>;
    bool HasRelationship<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>;
    bool CreateConstraint();
    bool CreateIndex();
    bool CreateIndex(string property);
    IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate);
    IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate, int limit, int skip);
    IQueryable<TEntity> Find(string label, string expression);
    IQueryable<TEntity> Find(string label, string expression, int limit, int skip);
    IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> expression);
    IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> expression, int limit, int skip);
}

// todo - add async methods for graph repository