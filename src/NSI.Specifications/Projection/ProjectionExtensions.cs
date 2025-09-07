using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NSI.Specifications.Projection;

/// <summary>
/// Extension methods to apply <see cref="IProjectionSpecification{TSource, TResult}"/>.
/// </summary>
public static class ProjectionExtensions
{
    /// <summary>
    /// Applies the projection to an <see cref="IQueryable{T}"/> using Queryable.Select
    /// (<see href="https://learn.microsoft.com/dotnet/api/system.linq.queryable.select">docs</see>).
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="spec">Projection specification.</param>
    /// <returns>The projected queryable.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="source"/> or <paramref name="spec"/> is null.</exception>
    public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, IProjectionSpecification<TSource, TResult> spec)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(spec);
        return Queryable.Select(source, spec.Selector);
    }

    /// <summary>
    /// Applies the projection to an <see cref="IEnumerable{T}"/> using a compiled selector.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="source">The enumerable source.</param>
    /// <param name="spec">Projection specification.</param>
    /// <returns>The projected enumerable.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="source"/> or <paramref name="spec"/> is null.</exception>
    public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, IProjectionSpecification<TSource, TResult> spec)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(spec);
        // Compile once for IEnumerable execution.
        var selector = spec.Selector.Compile();
        return source.Select(selector);
    }
}
