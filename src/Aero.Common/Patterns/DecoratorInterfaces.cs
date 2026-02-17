using Aero.Common.Commands;

namespace Aero.Common.Patterns;

public interface IDecorator : ICommand{}
public interface IDecorator<T> : ICommand<T>{}
public interface IDecorator<T, TReturn> : ICommand<T, TReturn>{}
    
public interface IAsyncDecorator : IAsyncCommand { }
public interface IAsyncDecorator<T> : IAsyncCommand<T>{}
public interface IAsyncDecorator<T, TResult> : IAsyncCommand<T, TResult>{}