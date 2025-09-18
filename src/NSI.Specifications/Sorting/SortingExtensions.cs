using System.Linq.Expressions;
using System.Reflection;

namespace NSI.Specifications.Sorting;

/// <summary>
/// High-level extension helpers to apply ordering described by an
/// <see cref="ISortSpecification{T}"/> over queryable and in‑memory sequences.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Translate a collection of <see cref="ISortClause{T}"/> instances into chained
///     <c>OrderBy/ThenBy</c> provider operations for <see cref="IQueryable{T}"/>.</description></item>
///   <item><description>Provide an in‑memory fallback for <see cref="IEnumerable{T}"/> using compiled
///     key selectors.</description></item>
///   <item><description>Preserve explicit precedence via each clause <see cref="ISortClause{T}.OrderIndex"/>.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Ensure clause indices are contiguous starting at 0 to avoid unintended gaps.</description></item>
///   <item><description>Prefer stable, deterministic keys (avoid volatile computed expressions).</description></item>
///   <item><description>Keep key selectors pure and provider‑translatable (no side effects).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Reflection cost of generic method lookup occurs once (static cached).</description></item>
///   <item><description>Enumerable path compiles each key selector exactly once per invocation.</description></item>
///   <item><description>Overall complexity O(n log n) dominated by downstream sort operations.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Methods are pure (no shared mutable state). Static cached metadata is
/// immutable.</para>
/// </remarks>
/// <example>
/// <code>
/// // Build specification (example)
/// var spec = new SortSpecification&lt;User&gt;()
///   .Then(u =&gt; u.LastName)
///   .Then(u =&gt; u.FirstName);
///
/// // Apply to EF Core query (provider translation)
/// var orderedQuery = context.Users.OrderBy(spec);
///
/// // Apply in-memory
/// var orderedList = userList.OrderBy(spec).ToList();
/// </code>
/// </example>
public static class SortingExtensions {
  /// <summary>
  /// Applies an <see cref="ISortSpecification{T}"/> to an <see cref="IQueryable{T}"/> source.
  /// </summary>
  /// <typeparam name="T">Entity element type.</typeparam>
  /// <param name="source">Queryable source (non-null).</param>
  /// <param name="specification">Sort specification; if null or empty no ordering applied.</param>
  /// <returns>Ordered queryable (deferred) or original source when no clauses supplied.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> is null.</exception>
  /// <remarks>
  /// <para>Reflection is used once per clause to invoke the appropriate generic LINQ method.
  /// Providers can translate the resulting expression tree to SQL or other backends.</para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var ordered = context.Users.OrderBy(userSortSpec);
  /// </code>
  /// </example>
  public static IQueryable<T> OrderBy<T>(
    this IQueryable<T> source,
    ISortSpecification<T>? specification) {
    ArgumentNullException.ThrowIfNull(source);
    if (specification == null || specification.Clauses.Count == 0) {
      return source;
    }

    IOrderedQueryable<T>? ordered = null;
    foreach (var clause in specification.Clauses.OrderBy(c => c.OrderIndex)) {
      var lambda = clause.KeySelector;
      var keyType = lambda.ReturnType;

      if (ordered == null) {
        var method = (clause.Direction == SortDirection.Asc ?
          QueryableOrderBy : QueryableOrderByDescending)
          .MakeGenericMethod(typeof(T), keyType);
        ordered = (IOrderedQueryable<T>)method.Invoke(null, [source, lambda])!;
      } else {
        var method = (clause.Direction == SortDirection.Asc ?
          QueryableThenBy : QueryableThenByDescending)
          .MakeGenericMethod(typeof(T), keyType);
        ordered = (IOrderedQueryable<T>)method.Invoke(null, [ordered, lambda])!;
      }
    }
    return ordered ?? source;
  }

  /// <summary>
  /// Applies an <see cref="ISortSpecification{T}"/> to an <see cref="IEnumerable{T}"/> source.
  /// </summary>
  /// <typeparam name="T">Element type.</typeparam>
  /// <param name="source">In-memory sequence (non-null).</param>
  /// <param name="specification">Sort specification; if null or empty the sequence is returned.</param>
  /// <returns>Lazily ordered sequence or original sequence when no clauses exist.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="source"/> is null.</exception>
  /// <remarks>
  /// <para>Each clause selector is compiled once; subsequent chaining uses reflection to invoke
  /// the appropriate <see cref="Enumerable"/> ordering method.</para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var ordered = users.OrderBy(userSortSpec).ToList();
  /// </code>
  /// </example>
  public static IEnumerable<T> OrderBy<T>(
    this IEnumerable<T> source,
    ISortSpecification<T>? specification) {
    ArgumentNullException.ThrowIfNull(source);
    if (specification == null || specification.Clauses.Count == 0) {
      return source;
    }

    IOrderedEnumerable<T>? ordered = null;
    foreach (var clause in specification.Clauses.OrderBy(c => c.OrderIndex)) {
      var lambda = clause.KeySelector;
      var keyType = lambda.ReturnType;
      // Compile once per clause for IEnumerable execution.
      var compiled = CompileLambda(lambda);

      if (ordered == null) {
        var method = clause.Direction == SortDirection.Asc ?
          EnumerableOrderBy : EnumerableOrderByDescending;
        ordered = (IOrderedEnumerable<T>)method
          .MakeGenericMethod(typeof(T), keyType)
          .Invoke(null, [source, compiled])!;
      } else {
        var method = clause.Direction == SortDirection.Asc ?
          EnumerableThenBy : EnumerableThenByDescending;
        ordered = (IOrderedEnumerable<T>)method
          .MakeGenericMethod(typeof(T), keyType)
          .Invoke(null, [ordered, compiled])!;
      }
    }
    return ordered ?? source;
  }

  // Cached Queryable method definitions (resolved once; generic parameters supplied at runtime).
  private static readonly MethodInfo QueryableOrderBy = typeof(Queryable).GetMethods()
    .First(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2);
  private static readonly MethodInfo QueryableOrderByDescending = typeof(Queryable).GetMethods()
    .First(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Length == 2);
  private static readonly MethodInfo QueryableThenBy = typeof(Queryable).GetMethods()
    .First(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Length == 2);
  private static readonly MethodInfo QueryableThenByDescending = typeof(Queryable).GetMethods()
    .First(m => m.Name == nameof(Queryable.ThenByDescending) && m.GetParameters().Length == 2);

  // Cached Enumerable method definitions.
  private static readonly MethodInfo EnumerableOrderBy = typeof(Enumerable).GetMethods()
    .First(m => m.Name == nameof(Enumerable.OrderBy) && m.GetParameters().Length == 2);
  private static readonly MethodInfo EnumerableOrderByDescending = typeof(Enumerable).GetMethods()
    .First(m => m.Name == nameof(Enumerable.OrderByDescending) && m.GetParameters().Length == 2);
  private static readonly MethodInfo EnumerableThenBy = typeof(Enumerable).GetMethods()
    .First(m => m.Name == nameof(Enumerable.ThenBy) && m.GetParameters().Length == 2);
  private static readonly MethodInfo EnumerableThenByDescending = typeof(Enumerable).GetMethods()
    .First(m => m.Name == nameof(Enumerable.ThenByDescending) && m.GetParameters().Length == 2);

  /// <summary>
  /// Compiles a lambda expression to a delegate (single call helper for clarity).
  /// </summary>
  private static object CompileLambda(LambdaExpression lambda) => lambda.Compile();
}
