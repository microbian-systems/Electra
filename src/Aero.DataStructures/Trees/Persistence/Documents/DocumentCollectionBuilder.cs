using System;
using System.Linq.Expressions;
using Aero.DataStructures.Trees.Persistence.Heap;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Serialization;

namespace Aero.DataStructures.Trees.Persistence.Documents;

public sealed class DocumentCollectionBuilder<TDocument>
    where TDocument : class
{
    private readonly DocumentIndexRegistry<TDocument> _registry = new();
    private Func<TDocument, Guid>? _idExtractor;
    private IDocumentSerializer<TDocument>? _serializer;

    public DocumentCollectionBuilder<TDocument> HasPrimaryKey(
        Expression<Func<TDocument, Guid>> keySelector)
    {
        _idExtractor = keySelector.Compile();
        return this;
    }

    public DocumentCollectionBuilder<TDocument> HasIndex<TField>(
        Expression<Func<TDocument, TField>> fieldSelector,
        Action<IndexOptions>? configure = null)
        where TField : unmanaged, IComparable<TField>
    {
        var options = new IndexOptions();
        configure?.Invoke(options);
        var fieldName = GetFieldName(fieldSelector);

        var def = new IndexDefinition<TDocument, TField>
        {
            Name = options.Name ?? fieldName,
            Type = options.IsUnique ? IndexType.Unique : IndexType.Secondary,
            IsUnique = options.IsUnique,
            IsDescending = options.IsDescending,
            FieldName = fieldName,
            FieldType = typeof(TField),
            KeyExtractor = fieldSelector.Compile(),
            KeyExpression = fieldSelector,
        };

        throw new NotImplementedException("Tree creation requires storage backend. Use the DI extensions.");
    }

    public DocumentCollectionBuilder<TDocument> UseSerializer(
        IDocumentSerializer<TDocument> serializer)
    {
        _serializer = serializer;
        return this;
    }

    public IDocumentCollection<TDocument> Build()
    {
        if (_idExtractor is null)
            throw new InvalidOperationException("Primary key must be configured via HasPrimaryKey.");

        _serializer ??= new SystemTextJsonSerializer<TDocument>();

        throw new NotImplementedException("Build requires storage backend. Use the DI extensions.");
    }

    internal DocumentIndexRegistry<TDocument> GetRegistry() => _registry;
    internal Func<TDocument, Guid>? GetIdExtractor() => _idExtractor;
    internal IDocumentSerializer<TDocument> GetSerializer() =>
        _serializer ?? new SystemTextJsonSerializer<TDocument>();

    private static string GetFieldName<TField>(Expression<Func<TDocument, TField>> expr) =>
        ((MemberExpression)expr.Body).Member.Name;
}

public sealed class IndexOptions
{
    public string? Name { get; set; }
    public bool IsUnique { get; set; }
    public bool IsDescending { get; set; }
}
