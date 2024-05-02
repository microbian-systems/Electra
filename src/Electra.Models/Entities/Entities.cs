using System;

namespace Electra.Models.Entities
{
    public interface IEntityInt : IEntity<int> { }
    
    public interface IEntityString : IEntity<string> {}
    
    public interface IEntityGuid : IEntity<Guid> {}
}