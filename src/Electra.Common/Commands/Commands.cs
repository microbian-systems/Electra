namespace Electra.Common.Commands
{
    /// <summary>
    /// Command pattern to be used as a base interface for specific ICommandX interfaces (see remarks)
    /// </summary>
    /// <remarks>example usage: ISpecificCommand : ICommand.
    /// can be combined with other generic ICommands to make it more robust
    /// </remarks>
    public interface IAsyncCommand
    {
        Task ExecuteAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Any type to be based to Execute method</typeparam>
    /// <remarks>can make the parameter optional to have it injected</remarks>
    public interface IAsyncCommand<in T>
    {
        Task ExecuteAsync(T parameter);
    }

    /// <summary>
    /// Command that takes a parameter and returns a value
    /// </summary>
    /// <typeparam name="T">Any type to be based to Execute method</typeparam>
    /// <typeparam name="TReturn">Expected return value of type TReturn</typeparam>
    /// <remarks>can make the parameter optional to have it injected</remarks>
    public interface IAsyncCommand<in T, TReturn>
    {
        Task<TReturn> ExecuteAsync(T parameter);
    }

    // public interface ICommand<in T>
    // {
    //     void Execute(T param);
    // }

    public interface ICommand<in T, out TReturn>
    {
        TReturn Execute(T param);
    }
}