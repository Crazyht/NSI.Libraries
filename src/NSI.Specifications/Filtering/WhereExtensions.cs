using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NSI.Specifications.Abstractions;
using NSI.Specifications.Optimization;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Applies specifications to queryables and enumerables, leveraging provider-specific optimizations when available.
/// </summary>
public static class WhereExtensions
{
    /// <summary>
    /// Applies a specification to IQueryable with optional provider-specific optimization.
    /// </summary>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        var provider = ProviderNameResolver.Resolve(source);
        var candidates = SpecOptimizationRegistry.Get(provider, specification.GetType());
        foreach (var opt in candidates)
        {
            var lambda = opt.TryRewriteLambda(specification);
            if (lambda is Expression<Func<T, bool>> typed)
            {
                return source.Where(typed);
            }
        }

        return source.Where(specification.ToExpression());
    }

    /// <summary>
    /// Applies a specification to in-memory enumerable (no optimization needed).
    /// </summary>
    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        var predicate = specification.ToExpression().Compile();
        return source.Where(predicate);
    }
}
