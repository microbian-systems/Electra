using System;

namespace Electra.Core.Entities;

    public interface IEntityInt : IEntity<int> { }
    
    public interface IEntityString : IEntity<string> {}
    
    public interface IEntityGuid : IEntity<Guid> {}