namespace NSI.Specifications.Include;

/// <summary>
/// Extension helpers to apply <see cref="IIncludeSpecification{T}"/> navigation graphs to query
/// sources (typed chains for <see cref="IQueryable{T}"/>, no-op for <see cref="IEnumerable{T}"/>).
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Route specification application to reflective helper for EF Core queryables.</description></item>
///   <item><description>Provide a documented no-op for in-memory sequences (includes are query provider
///     concerns only).</description></item>
/// </list>
/// </para>
/// <para>Performance: Application cost proportional to total include steps (reflection per step).
/// In typical read-model scenarios chains are shallow (1-3 depth).</para>
/// <para>Thread-safety: Methods are pure; specifications expected to be immutable.</para>
/// </remarks>
/// <example>
/// <code>
/// IIncludeSpecification&lt;Order&gt; spec = new OrderGraphInclude();
/// var enriched = db.Orders.Include(spec); // IQueryable path (EF Core)
/// var snapshot = orderList.Include(spec); // IEnumerable path (no-op)
/// </code>
/// </example>
public static class IncludeExtensions {
  /// <summary>
  /// Applies the include specification to the queryable using EF Core Include / ThenInclude
  /// semantics (typed chains first, then string paths).
  /// </summary>
  /// <typeparam name="T">Root entity type.</typeparam>
  /// <param name="source">Queryable source (non-null).</param>
  /// <param name="spec">Include specification aggregating chains and string paths (non-null).</param>
  /// <returns>Queryable with all specification includes applied.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> or
  /// <paramref name="spec"/> is null.</exception>
  public static IQueryable<T> Include<T>(this IQueryable<T> source, IIncludeSpecification<T> spec)
    where T : class {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(spec);
    return IncludeExpressionHelper.Apply(source, spec);
  }

  /// <summary>
  /// Returns the original sequence unchanged (includes are a query provider concern and have no
  /// effect on already materialized in-memory collections).
  /// </summary>
  /// <typeparam name="T">Element type.</typeparam>
  /// <param name="source">In-memory sequence (non-null).</param>
  /// <param name="spec">Specification (ignored, evaluated only on IQueryable).</param>
  /// <returns>The original <paramref name="source"/> sequence.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> or
  /// <paramref name="spec"/> is null.</exception>
  public static IEnumerable<T> Include<T>(this IEnumerable<T> source, IIncludeSpecification<T> spec) {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(spec);
    _ = spec; // explicit acknowledgment to avoid unused parameter warning without suppression
    return source;
  }
}
