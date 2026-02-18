namespace Aero.Elastic;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class IndexNameAttribute : Attribute
{
    public string IndexName { get; }

    public IndexNameAttribute(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ArgumentNullException($"{nameof(indexName)} cannot be null");

        IndexName = indexName;
    }
}