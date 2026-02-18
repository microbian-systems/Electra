using System;
using System.Linq.Expressions;

namespace Aero.DataStructures.Trees.Persistence.Indexes;

public class IndexDefinition
{
    public string Name { get; init; } = string.Empty;
    public IndexType Type { get; init; }
    public bool IsUnique { get; init; }
    public bool IsDescending { get; init; }
    public Type FieldType { get; init; } = typeof(object);
    public string FieldName { get; init; } = string.Empty;
    public long RootPageId { get; set; } = -1;
    public int StringKeyWidth { get; init; }
    public bool IsStringIndex => StringKeyWidth > 0;
}

public sealed class IndexDefinition<TDocument, TField> : IndexDefinition
    where TField : unmanaged, IComparable<TField>
{
    public required Func<TDocument, TField> KeyExtractor { get; init; }
    public required Expression<Func<TDocument, TField>> KeyExpression { get; init; }
}
