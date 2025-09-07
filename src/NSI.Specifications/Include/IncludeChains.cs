using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Include;

/// <summary>
/// Factory for creating include chains.
/// </summary>
public static class IncludeChains
{
    /// <summary>
    /// Creates an include chain from a sequence of member access expressions.
    /// </summary>
    public static IIncludeChain<T> For<T>(params LambdaExpression[] steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Length == 0)
        {
            throw new ArgumentException("At least one step is required", nameof(steps));
        }
        return new IncludeChain<T>(steps);
    }
}
