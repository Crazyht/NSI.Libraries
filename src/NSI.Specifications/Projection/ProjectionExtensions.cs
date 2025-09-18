namespace NSI.Specifications.Projection;

/// <summary>
/// Extension helpers to apply <see cref="IProjectionSpecification{TSource, TResult}"/> projections
/// to queryable or in-memory sequences in a consistent, analyzable manner.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Delegate translation-capable selector expressions to LINQ providers
///     (<see cref="Queryable.Select{TSource, TResult}(System.Linq.IQueryable{TSource}, System.Linq.Expressions.Expression{System.Func{TSource, TResult}})"/>).</description></item>
///   <item><description>Provide an in-memory fallback for <see cref="IEnumerable{T}"/> via compiled
///     delegates.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer the <c>IQueryable</c> overload for database translation.</description></item>
///   <item><description>Avoid compiling repeatedly inside tight loops; compose then evaluate.</description></item>
///   <item><description>Ensure <see cref="IProjectionSpecification{TSource, TResult}.Selector"/> remains
///     pure and side‑effect free.</description></item>
/// </list>
/// </para>
/// <para>Performance: The enumerable overload compiles once per call; cache projection specifications
/// externally (e.g. DI singleton) to avoid repeated expression allocation.</para>
/// <para>Thread-safety: Methods are pure; projection specifications should be immutable.</para>
/// </remarks>
/// <example>
/// <code>
/// var projection = new UserSummaryProjection();
/// // Provider translation
/// var query = context.Users.Select(projection);
/// // In-memory
/// var list = userList.Select(projection).ToList();
/// </code>
/// </example>
public static class ProjectionExtensions {
  /// <summary>
  /// Applies the projection to an <see cref="IQueryable{T}"/> using
  /// <see cref="Queryable.Select{TSource, TResult}(System.Linq.IQueryable{TSource}, System.Linq.Expressions.Expression{System.Func{TSource, TResult}})"/>.
  /// </summary>
  /// <typeparam name="TSource">Source element type.</typeparam>
  /// <typeparam name="TResult">Projected result type.</typeparam>
  /// <param name="source">Queryable source (non-null).</param>
  /// <param name="spec">Projection specification (non-null).</param>
  /// <returns>Queryable with the projection applied (deferred execution).</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> or
  /// <paramref name="spec"/> is null.</exception>
  /// <remarks>
  /// <para>Expression is passed directly enabling provider translation (SQL, etc.).</para>
  /// <para>Composition: <c>source.Where(filter).Select(spec)</c>.</para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var projected = context.Orders.Select(new OrderSummaryProjection());
  /// var list = await projected.ToListAsync(ct);
  /// </code>
  /// </example>
  public static IQueryable<TResult> Select<TSource, TResult>(
    this IQueryable<TSource> source,
    IProjectionSpecification<TSource, TResult> spec) {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(spec);
    return Queryable.Select(source, spec.Selector);
  }

  /// <summary>
  /// Applies the projection to an <see cref="IEnumerable{T}"/> by compiling the selector
  /// expression once and invoking it for each element.
  /// </summary>
  /// <typeparam name="TSource">Source element type.</typeparam>
  /// <typeparam name="TResult">Projected result type.</typeparam>
  /// <param name="source">In-memory sequence (non-null).</param>
  /// <param name="spec">Projection specification (non-null).</param>
  /// <returns>Lazily evaluated projected sequence.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> or
  /// <paramref name="spec"/> is null.</exception>
  /// <remarks>
  /// <para>Compilation occurs per call; cache <paramref name="spec"/> externally for reuse.</para>
  /// <para>Suitable for post-materialization transformations.</para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var summaries = userList.Select(new UserSummaryProjection()).ToArray();
  /// </code>
  /// </example>
  public static IEnumerable<TResult> Select<TSource, TResult>(
    this IEnumerable<TSource> source,
    IProjectionSpecification<TSource, TResult> spec) {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(spec);
    var selector = spec.Selector.Compile(); // Compile once for enumeration
    return source.Select(selector);
  }
}
