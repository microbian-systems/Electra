using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Aero.DataStructures.Trees.Persistence.Linq.Translation;

public static class ExpressionEvaluator
{
    public static object Evaluate(Expression expr)
    {
        switch (expr)
        {
            case ConstantExpression constant:
                return constant.Value!;

            case MemberExpression member when member.Expression is ConstantExpression closure:
                return member.Member switch
                {
                    FieldInfo fi => fi.GetValue(((ConstantExpression)member.Expression).Value) ?? throw new InvalidOperationException("Null constant in query."),
                    PropertyInfo pi => pi.GetValue(((ConstantExpression)member.Expression).Value) ?? throw new InvalidOperationException("Null constant in query."),
                    _ => CompileAndInvoke(expr)
                };

            case UnaryExpression { NodeType: ExpressionType.Convert } convert:
                var inner = Evaluate(convert.Operand);
                return Convert.ChangeType(inner, convert.Type);

            default:
                return CompileAndInvoke(expr);
        }
    }

    private static object CompileAndInvoke(Expression expr)
    {
        try
        {
            return Expression.Lambda(expr).Compile().DynamicInvoke()
                   ?? throw new InvalidOperationException("Expression evaluated to null.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new NotSupportedInQueryException(
                $"Cannot evaluate expression: {expr}", ex);
        }
    }
}

public sealed class NotSupportedInQueryException : Exception
{
    public NotSupportedInQueryException(string message, Exception? inner = null)
        : base(message, inner) { }
}
