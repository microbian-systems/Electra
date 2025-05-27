using System;
using System.Linq.Expressions;

namespace Electra.Common.Extensions;

public static class FuncExtensions
{
    public static Expression<Func<T, bool>> FuncToExpression<T>(Func<T, bool> func)  
    {  
        return x => func(x);  
    } 
}