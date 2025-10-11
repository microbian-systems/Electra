namespace Electra.Persistence.DynamoDB;

/// <summary>
/// Represents a table-name for a DynamoDB table. We have to do this
/// due to a environment specific table naming convention - i.e. -
/// svt-{env}-tablename
/// </summary>
public class DynamoTableNameAttribute : Attribute
{
    public string Name { get; set; }

    public DynamoTableNameAttribute(String name)
    {
        Name = name;
    }
}