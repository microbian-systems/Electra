using System;
using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents an Expression Tree for parsing and evaluating mathematical expressions.
/// </summary>
public class ExpressionTree
{
    public ExpressionNode Root { get; private set; }

    /// <summary>
    /// Builds an expression tree from a postfix expression string.
    /// Example: "3 4 + 2 *" corresponds to (3 + 4) * 2
    /// </summary>
    /// <param name="postfix">The postfix expression.</param>
    public void Build(string postfix)
    {
        var stack = new Stack<ExpressionNode>();
        var tokens = postfix.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (double.TryParse(token, out var value))
            {
                stack.Push(new ExpressionNode(value));
            }
            else
            {
                var right = stack.Pop();
                var left = stack.Pop();
                stack.Push(new ExpressionNode(token[0], left, right));
            }
        }
        Root = stack.Pop();
    }

    /// <summary>
    /// Evaluates the expression tree.
    /// </summary>
    /// <returns>The result of the expression.</returns>
    public double Evaluate()
    {
        return Evaluate(Root);
    }

    private double Evaluate(ExpressionNode node)
    {
        if (node.Type == NodeType.Value)
        {
            return node.Value;
        }

        var left = Evaluate(node.Left);
        var right = Evaluate(node.Right);

        return node.Operator switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' => left / right,
            _ => throw new InvalidOperationException($"Invalid operator: {node.Operator}")
        };
    }
}