using System;

namespace Electra.Core.Extensions;

public static class Numbers
{
    public static decimal RaiseTo(decimal start, decimal nearest)
    {
        return Math.Ceiling(start / nearest) * nearest;
    }
}