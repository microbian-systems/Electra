using System.Linq.Expressions;
using Aero.Core.Data;
using Aero.Core.DataStructures.Graphs;
using Aero.Core.Entities;

namespace Aero.Services;

public class GraphService<TEntity, TKey> : IGraphService<TEntity, TKey> 
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    protected readonly IGraphRepository<TEntity, TKey> _repository;

    public GraphService(IGraphRepository<TEntity, TKey> repository)
    {
        _repository = repository;
    }

    public TEntity Add(TEntity entity)
    {
        // todo - fix add in grapchservice
        //return _repository.ad(entity);

        throw new NotImplementedException();
    }

    public Task<TEntity> AddAsync(TEntity entity)
    {
        return Task.FromResult(Add(entity));
    }

    public bool AddLabel(TKey id, string label)
    {
        return _repository.AddLabel(id, label);
    }

    public Task<bool> AddLabelAsync(TKey id, string label)
    {
        return Task.FromResult(AddLabel(id, label));
    }

    public bool AddRelationShip<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return _repository.AddRelationShip<TOut>(inboundId, outboundId, relationship);
    }

    public Task<bool> AddRelationShipAsync<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return Task.FromResult(AddRelationShip<TOut>(inboundId, outboundId, relationship));
    }

    public bool CreateConstraint()
    {
        return _repository.CreateConstraint();
    }

    public Task<bool> CreateConstraintAsync()
    {
        return Task.FromResult(CreateConstraint());
    }

    public bool CreateIndex()
    {
        return _repository.CreateIndex();
    }

    public bool CreateIndex(string property)
    {
        return _repository.CreateIndex(property);
    }

    public IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> Find(string label, string expression)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> Find(string expression)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateIndexAsync()
    {
        return Task.FromResult(CreateIndex());
    }

    public Task<bool> CreateIndexAsync(string property)
    {
        return Task.FromResult(CreateIndex(property));
    }

    public void Delete(TEntity entity)
    {
        // todo - fix delete method in graph service    
        throw new NotImplementedException();
        //_repository.Delete(entity);
    }

    public Task DeleteAsync(TEntity entity)
    {
        return Task.Run(() => Delete(entity));
    }

    public bool DeleteLabel(TKey id, string label)
    {
        return _repository.DeleteLabel(id, label);
    }

    public Task<bool> DeleteLabelAsync(TKey id, string label)
    {
        return Task.FromResult(DeleteLabel(id, label));
    }

    public bool DeleteRelationShip<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return _repository.DeleteRelationShip<TOut>(inboundId, outboundId, relationship);
    }

    public Task<bool> DeleteRelationShipAsync<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return Task.FromResult(DeleteRelationShip<TOut>(inboundId, outboundId, relationship));
    }

    //public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
    //{
    //    return _repository.Find(predicate);
    //}

    //public Task<IQueryable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    //{
    //    return Task.FromResult(Find(predicate));
    //}

    //public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> expression)
    //{
    //    return _repository.Find(expression);
    //}

    //public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> label, Expression<Func<TEntity, bool>> predicate)
    //{
    //    return _repository.Find(label, predicate);
    //}

    //public IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> expression)
    //{
    //    return _repository.Find(label, expression);
    //}

    //public Task<IQueryable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression)
    //{
    //    return Task.FromResult(Find(expression));
    //}

    //public Task<IQueryable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> label, Expression<Func<TEntity, bool>> predicate)
    //{
    //    return Task.FromResult(Find(label, predicate));
    //}

    //public Task<IQueryable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> label, string expression)
    //{
    //    return Task.FromResult(Find(label, expression));
    //}

    // todo - implement get methods in graph service
    public IEnumerable<TEntity> Get()
    {
        throw new NotImplementedException();
        //return _repository.Get();
    }

    public IEnumerable<TEntity> Get(int limit, int skip)
    {
        throw new NotImplementedException();
        //return _repository.Get();
    }

    public Task<IEnumerable<TEntity>> GetAsync()
    {
        return Task.FromResult(Get());
    }

    public Task<IEnumerable<TEntity>> GetAsync(int limit, int skip)
    {
        return Task.FromResult(Get(limit, skip));
    }

    public TEntity GetById(TKey id)
    {
        throw new NotImplementedException();
        //return _repository.GetById(id);
    }

    public Task<TEntity> GetByIdAsync(TKey id)
    {
        return Task.FromResult(GetById(id));
    }

    public IEnumerable<TEntity> Get(string label)
    {
        return _repository.Get(label);
    }

    public Task<IEnumerable<TEntity>> GetAsync(string label)
    {
        return Task.FromResult(Get(label));
    }

    public IEnumerable<string> GetLabels(TKey id)
    {
        return _repository.GetLabels(id);
    }

    public Task<IEnumerable<string>> GetLabelsAsync(TKey id)
    {
        return Task.FromResult(GetLabels(id));
    }

    public IEnumerable<TOut> GetRelated<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return _repository.GetRelated<TOut>(id, relationship);
    }

    public Task<IEnumerable<TOut>> GetRelatedAsync<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return Task.FromResult(GetRelated<TOut>(id, relationship));
    }

    public int GetRelatedCount<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return _repository.GetRelatedCount<TOut>(id, relationship);
    }

    public Task<int> GetRelatedCountAsync<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return Task.FromResult(GetRelatedCount<TOut>(id, relationship));
    }

    public TEntity Update(TEntity entity)
    {
        throw new NotImplementedException();
        //return _repository.Update(entity);
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        return Task.FromResult(Update(entity));
    }

    public IEnumerable<TOut> GetRelated<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey>, IEquatable<TKey> where TRelation : class
    {
        return _repository.GetRelated<TOut, TRelation>(id, relationship, predicate);
    }

    public int GetRelatedCount<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey>, IEquatable<TKey> where TRelation : class
    {
        return _repository.GetRelatedCount<TOut, TRelation>(id, relationship, predicate);
    }

    public bool AddRelationShip<TOut, TRelation>(TKey inboundId, TKey outboundId, string relationship, TRelation relation) where TOut : class, IEntity<TKey>, IEquatable<TKey> where TRelation : class
    {
        return _repository.AddRelationShip<TOut, TRelation>(inboundId, outboundId, relationship, relation);
    }

    public bool HasRelationship<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return _repository.HasRelationship<TOut>(inboundId, outboundId, relationship);
    }

    public Task<IEnumerable<TOut>> GetRelatedAsync<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey>, IEquatable<TKey> where TRelation : class
    {
        return Task.FromResult(GetRelated<TOut, TRelation>(id, relationship, predicate));
    }

    public Task<int> GetRelatedCountAsync<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey>, IEquatable<TKey> where TRelation : class
    {
        return Task.FromResult(GetRelatedCount<TOut, TRelation>(id, relationship, predicate));
    }

    public Task<bool> AddRelationShipAsync<TOut, TRelation>(TKey inboundId, TKey outboundId, string relationship, TRelation relation) where TOut : class, IEntity<TKey>, IEquatable<TKey> where TRelation : class
    {
        return Task.FromResult(AddRelationShip<TOut, TRelation>(inboundId, outboundId, relationship, relation));
    }

    public Task<bool> HasRelationshipAsync<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>, IEquatable<TKey>
    {
        return Task.FromResult(HasRelationship<TOut>(inboundId, outboundId, relationship));
    }

    public IEnumerable<TEntity> Get(string label, int limit, int skip)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate, int limit, int skip)
    {
        return _repository.Find(label, predicate, limit, skip);
    }

    public IQueryable<TEntity> Find(string label, string expression, int limit, int skip)
    {
        return _repository.Find(label, expression, limit, skip);
    }

    public IQueryable<TEntity> Find(string expression, int limit, int skip)
    {
        throw new NotImplementedException();
        // return _repository.Find(expression, limit, skip);
    }

    public Task<IEnumerable<TEntity>> GetAsync(string label, int limit, int skip)
    {
        return Task.FromResult(Get(label, limit, skip));
    }

    public Task<IQueryable<TEntity>> FindAsync(string label, Expression<Func<TEntity, bool>> predicate, int limit, int skip)
    {
        return Task.FromResult(Find(label, predicate, limit, skip));
    }

    public Task<IQueryable<TEntity>> FindAsync(string label, string expression, int limit, int skip)
    {
        return Task.FromResult(Find(label, expression, limit, skip));
    }

    public Task<IQueryable<TEntity>> FindAsync(string expression, int limit, int skip)
    {
        return Task.FromResult(Find(expression, limit, skip));
    }
}