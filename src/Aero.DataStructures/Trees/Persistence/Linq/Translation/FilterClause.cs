using System.Linq.Expressions;
using Aero.DataStructures.Trees.Persistence.Indexes;

namespace Aero.DataStructures.Trees.Persistence.Linq.Translation;

public abstract record FilterClause
{
    public abstract bool IsIndexSatisfiable { get; }
}

public sealed record ComparisonFilter(
    MemberAccess Member,
    ExpressionType Operator,
    object Value
) : FilterClause
{
    public override bool IsIndexSatisfiable =>
        Member.IndexDefinition is not null && IsSupportedOperator;

    private bool IsSupportedOperator => Operator is
        ExpressionType.Equal or
        ExpressionType.NotEqual or
        ExpressionType.GreaterThan or
        ExpressionType.GreaterThanOrEqual or
        ExpressionType.LessThan or
        ExpressionType.LessThanOrEqual;
}

public sealed record AndFilter(FilterClause Left, FilterClause Right) : FilterClause
{
    public override bool IsIndexSatisfiable => Left.IsIndexSatisfiable || Right.IsIndexSatisfiable;
}

public sealed record OrFilter(FilterClause Left, FilterClause Right) : FilterClause
{
    public override bool IsIndexSatisfiable => false;
}

public sealed record NotFilter(FilterClause Inner) : FilterClause
{
    public override bool IsIndexSatisfiable => false;
}

public sealed record MethodFilter(
    MemberAccess Target,
    string MethodName,
    IReadOnlyList<object> Arguments
) : FilterClause
{
    public override bool IsIndexSatisfiable =>
        MethodName is "StartsWith" && Target.IndexDefinition is not null;
}

public sealed record MemberAccess(
    string FieldName,
    IndexDefinition? IndexDefinition
);

public sealed record OrderClause(
    MemberAccess Member,
    bool Descending
);
