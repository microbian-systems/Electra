namespace Electra.DataStructures.Trees;

public enum NodeType
{
    Value,
    Operator
}
    
/// <summary>
/// Represents a node in an Expression Tree.
/// </summary>
public class ExpressionNode
{
    public NodeType Type { get; }
    public double Value { get; }
    public char Operator { get; }
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }

    // Constructor for value nodes
    public ExpressionNode(double value)
    {
        Type = NodeType.Value;
        Value = value;
    }

    // Constructor for operator nodes
    public ExpressionNode(char op, ExpressionNode left, ExpressionNode right)
    {
        Type = NodeType.Operator;
        Operator = op;
        Left = left;
        Right = right;
    }
}