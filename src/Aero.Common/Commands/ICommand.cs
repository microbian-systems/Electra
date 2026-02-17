using System.Threading.Tasks;

namespace Aero.Common.Commands;
// todo - replace these commands with MediatR
// public interface ICommand
// {
//     void Execute();
//     void Execute<T>(T param);
//     Task ExecuteAsync();
//     Task ExecuteAsync<T>(T param);
// }
//
// public interface ICommand<T>
// {
//     T Execute();
//     T Execute<P>(P param);
//     Task<T> ExecuteAsync();
//     Task<T> ExecuteAsync<P>(P param);
// }

public interface ICommandAsync<T, TReturn>
{
    Task<TReturn> ExecuteAsync(T param);
}
    
public interface ICommandAsync<T>
{
    Task ExecuteAsync(T param);
}
    
public interface ICommandAsync
{
    Task ExecuteAsync();
}

public interface ICommand<T> 
{
    void Execute(T param);
}

public interface ICommand
{
    void Execute();
}
    
/// <summary>
/// Represents a param for the command pattern
/// </summary>
public interface ICommandParameter
{
}