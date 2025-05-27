
using Microsoft.Extensions.Logging;

namespace Electra.Common.Patterns;

public abstract class AbstractDecorator : IDecorator
{
    private readonly ILogger log;
    public AbstractDecorator(ILogger log) => this.log = log;
    public abstract void Execute();
}

public abstract class AbstractDecorator<T> : IDecorator<T>
{
    private readonly ILogger log;
    public AbstractDecorator(ILogger log) => this.log = log;
    public abstract void Execute(T param);
}
    
public abstract class AbstractDecorator<T, TReturn> : IDecorator<T, TReturn>
{
    private readonly ILogger log;
    public AbstractDecorator(ILogger log) => this.log = log;
    public abstract TReturn Execute(T param);
}