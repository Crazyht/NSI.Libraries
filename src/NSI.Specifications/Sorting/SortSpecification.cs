using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Immutable ordered collection of sorting clauses for entity type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Entity type whose instances will be ordered.</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Aggregate multiple <see cref="ISortClause{T}"/> instances with explicit
///     precedence indices.</description></item>
///   <item><description>Provide fluent immutable composition via <see cref="Then{TKey}(Expression{Func{T, TKey}}, SortDirection)"/>.</description></item>
///   <item><description>Act as reusable, cacheable ordering intent passed to query adapters.</description></item>
/// </list>
/// </para>
/// <para>Immutability: Each compositional operation returns a new instance; previous instances are
/// never mutated, enabling safe reuse across threads.</para>
/// <para>Performance: Construction is O(n) for clause copy on each <see cref="Then{TKey}(Expression{Func{T, TKey}}, SortDirection)"/> call. Typical
/// specifications have a small number of clauses; overhead is negligible compared to provider
/// translation or actual sort operations.</para>
/// <para>Thread-safety: Fully immutable after creation.</para>
/// </remarks>
/// <example>
/// <code>
/// // Build a multi-level ordering: LastName ASC, FirstName ASC, CreatedUtc DESC
/// var spec = Sort.Create&lt;User, string&gt;(u => u.LastName)
///   .Then(u => u.FirstName)
///   .Then(u => u.CreatedUtc, SortDirection.Desc);
///
/// // Apply (IQueryable) -> translated to SQL ORDER BY
/// var orderedQuery = context.Users.OrderBy(spec);
///
/// // Apply (IEnumerable) in-memory
/// var orderedList = users.OrderBy(spec).ToList();
/// </code>
/// </example>
/// <seealso cref="ISortSpecification{T}"/>
/// <seealso cref="ISortClause{T}"/>
/// <seealso cref="SortDirection"/>
public sealed class SortSpecification<T>: ISortSpecification<T> {
  /// <inheritdoc />
  public IReadOnlyList<ISortClause<T>> Clauses { get; }

  /// <summary>
  /// Private constructor preserving immutability.
  /// </summary>
  /// <param name="clauses">Ordered immutable list of clauses (non-null).</param>
  private SortSpecification(IReadOnlyList<ISortClause<T>> clauses) => Clauses = clauses;

  /// <summary>
  /// Creates a specification with a single initial clause (internal factory for helpers).
  /// </summary>
  /// <typeparam name="TKey">Key selector return type.</typeparam>
  /// <param name="keySelector">Non-null key selector.</param>
  /// <param name="direction">Sort direction.</param>
  /// <returns>New single-clause specification.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="keySelector"/> is null.</exception>
  internal static SortSpecification<T> FromSingle<TKey>(
    Expression<Func<T, TKey>> keySelector,
    SortDirection direction) {
    ArgumentNullException.ThrowIfNull(keySelector);
    ISortClause<T>[] clauses = [new SortClause<T, TKey>(0, direction, keySelector)];
    return new SortSpecification<T>(clauses);
  }

  /// <summary>
  /// Adds an additional clause returning a new specification instance (immutability preserved).
  /// </summary>
  /// <typeparam name="TKey">Key selector return type.</typeparam>
  /// <param name="keySelector">Non-null key selector expression.</param>
  /// <param name="direction">Sort direction (defaults to <see cref="SortDirection.Asc"/>).</param>
  /// <returns>New specification containing prior clauses plus the appended one.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="keySelector"/> is null.</exception>
  /// <example>
  /// <code>
  /// var spec = Sort.Create&lt;User, string&gt;(u => u.LastName)
  ///   .Then(u => u.FirstName)
  ///   .Then(u => u.CreatedUtc, SortDirection.Desc);
  /// </code>
  /// </example>
  public SortSpecification<T> Then<TKey>(
    Expression<Func<T, TKey>> keySelector,
    SortDirection direction = SortDirection.Asc) {
    ArgumentNullException.ThrowIfNull(keySelector);
    var nextIndex = Clauses.Count;
    var list = new List<ISortClause<T>>(Clauses.Count + 1);
    list.AddRange(Clauses);
    list.Add(new SortClause<T, TKey>(nextIndex, direction, keySelector));
    return new SortSpecification<T>(list);
  }
}

