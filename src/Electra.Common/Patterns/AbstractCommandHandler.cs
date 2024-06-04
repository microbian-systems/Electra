using Electra.Common.Commands;

namespace Electra.Common.Patterns;

public abstract class AbstractCommandHandler(ILogger<AbstractCommandHandler> log)
    : ICommand
{
    protected readonly ILogger<AbstractCommandHandler> log = log;
    public abstract void Execute();
}

public abstract class AbstractCommandHandler<T>(ILogger<AbstractCommandHandler<T>> log)
    : ICommand<T>
{
    protected readonly ILogger<AbstractCommandHandler<T>> log = log;
    public abstract void Execute(T param);

    public void Execute(ICommandParameter param)
    {
        throw new System.NotImplementedException();
    }
}

public abstract class AbstractCommandHandler<T, TReturn>(ILogger<AbstractCommandHandler<T, TReturn>> log)
    : ICommand<T, TReturn>
{
    protected readonly ILogger<AbstractCommandHandler<T, TReturn>> log = log;
    public abstract TReturn Execute(T param);
}