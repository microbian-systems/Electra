using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Aero.DataStructures.Trees.Persistence.Documents;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Linq.Planning;
using Aero.DataStructures.Trees.Persistence.Linq.Translation;

namespace Aero.DataStructures.Trees.Persistence.Linq;

public sealed class DocumentQueryProvider<TDocument> : IQueryProvider
    where TDocument : class
{
    private readonly IDocumentCollection<TDocument> _collection;
    private readonly IDocumentIndexRegistry<TDocument> _registry;
    private readonly QueryTranslator<TDocument> _translator;
    private readonly QueryPlanner<TDocument> _planner;

    public DocumentQueryProvider(
        IDocumentCollection<TDocument> collection,
        IDocumentIndexRegistry<TDocument> registry,
        IQueryDiagnostics? diagnostics = null)
    {
        _collection = collection;
        _registry = registry;
        _translator = new QueryTranslator<TDocument>(registry);
        _planner = new QueryPlanner<TDocument>(registry, diagnostics);
    }

    public IQueryable CreateQuery(Expression expression) =>
        new DocumentQueryable<TDocument>(this, expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(TDocument))
            throw new NotSupportedInQueryException(
                "Type change via Select is not supported.");
        return (IQueryable<TElement>)(object)
            new DocumentQueryable<TDocument>(this, expression);
    }

    public object? Execute(Expression expression) =>
        Execute<IEnumerable<TDocument>>(expression);

    public TResult Execute<TResult>(Expression expression)
    {
        var query = _translator.Translate(expression);
        var plan = _planner.Plan(query);
        var docs = plan.ExecuteAsync(_collection, CancellationToken.None)
            .ToBlockingEnumerable()
            .ToList();
        return (TResult)(object)docs;
    }

    public IAsyncEnumerable<TDocument> ExecuteAsync(
        Expression expression,
        CancellationToken ct = default)
    {
        var query = _translator.Translate(expression);
        var plan = _planner.Plan(query);
        return plan.ExecuteAsync(_collection, ct);
    }
}
