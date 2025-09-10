namespace Electra.Common.Patterns;

public interface IVisitable
{
    void Accept(IVisitor visitor);
}

public interface IVisitable<out TReturn>
{
    TReturn Accept(IVisitor visitor);
}

public interface IVisitor
{
    void Visit(object visited);
}
    
/// <summary>
/// Visits and potentially modifies a type T
/// </summary>
/// <typeparam name="T">Any type to be visited</typeparam>
public interface IVisitor<in T> : IVisitor
{
    void Visit(T visited);
}

/// <summary>
/// Visits a type T and returns type TReturn
/// </summary>
/// <typeparam name="T">Type to be visited</typeparam>
/// <typeparam name="TReturn">any type that is desired. (try with tuples)</typeparam>
public interface IVisitor<in T, out TReturn> : IVisitor
{
    TReturn Visit(T visited);
}