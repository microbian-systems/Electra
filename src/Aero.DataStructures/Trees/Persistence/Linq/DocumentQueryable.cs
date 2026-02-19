using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Linq.Planning;
using Aero.DataStructures.Trees.Persistence.Linq.Translation;

namespace Aero.DataStructures.Trees.Persistence.Linq;

public sealed class DocumentQueryable<TDocument> : IQueryable<TDocument>
    where TDocument : class
{
    internal DocumentQueryProvider<TDocument> TypedProvider { get; }

    public DocumentQueryable(DocumentQueryProvider<TDocument> provider)
    {
        TypedProvider = provider;
        Expression = Expression.Constant(this);
    }

    internal DocumentQueryable(
        DocumentQueryProvider<TDocument> provider,
        Expression expression)
    {
        TypedProvider = provider;
        Expression = expression;
    }

    public Type ElementType => typeof(TDocument);
    public Expression Expression { get; }
    public IQueryProvider Provider => TypedProvider;

    public IEnumerator<TDocument> GetEnumerator() =>
        TypedProvider
            .Execute<IEnumerable<TDocument>>(Expression)
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
