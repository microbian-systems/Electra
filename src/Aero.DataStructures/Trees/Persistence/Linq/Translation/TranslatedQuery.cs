using System;
using System.Collections.Generic;
using Aero.DataStructures.Trees.Persistence.Indexes;

namespace Aero.DataStructures.Trees.Persistence.Linq.Translation;

public sealed class TranslatedQuery<TDocument>
{
    public IReadOnlyList<FilterClause> Filters { get; init; } = Array.Empty<FilterClause>();
    public IReadOnlyList<OrderClause> OrderBys { get; init; } = Array.Empty<OrderClause>();
    public int? Take { get; init; }
    public int? Skip { get; init; }
    public Func<TDocument, bool>? ResidualPredicate { get; set; }
    public IndexScanSpec? SelectedIndexScan { get; set; }
}

public sealed class IndexScanSpec
{
    public required IndexDefinition Index { get; init; }
    public object? From { get; init; }
    public object? To { get; init; }
    public bool IsPoint { get; init; }
    public bool IncludeFrom { get; init; } = true;
    public bool IncludeTo { get; init; } = true;
}
