using Electra.Common.Commands;

namespace Electra.Common.Caching.Decorators;

public interface ICachingCommandDecoratorAsync<T> : ICommandAsync<T>{}
public interface ICachingCommandDecoratorSync<T> : ICommand<T>{}
    
    
public interface ICachingCommandDecorator<T> : ICachingCommandDecoratorSync<T>, ICachingCommandDecoratorAsync<T>
{
}