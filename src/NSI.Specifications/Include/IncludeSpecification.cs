namespace NSI.Specifications.Include;

/// <summary>
/// Immutable include specification aggregating strongly typed navigation chains and optional
/// string-based include paths for a given root entity type.
/// </summary>
/// <typeparam name="T">Root entity type whose navigation graph is described.</typeparam>
/// <param name="chains">Optional initial typed include chains (may be null / empty).</param>
/// <param name="stringPaths">Optional initial string include paths (may be null / empty).</param>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="Chains"/> contain strongly typed, analyzer-friendly navigation
///     chains (preferred).</description></item>
///   <item><description><see cref="StringPaths"/> contain raw provider paths (fallback / dynamic
///     scenarios).</description></item>
///   <item><description>Typed chains are always applied before string paths.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Favor typed chains for refactoring safety and compile-time checks.</description></item>
///   <item><description>Reserve string paths for conditional / late-bound graph building.</description></item>
///   <item><description>Avoid redundant chains / paths to reduce query noise.</description></item>
/// </list>
/// </para>
/// <para>Performance: Construction copies inputs to compact immutable arrays (O(n)). Append
/// operations allocate a new specification instance with extended arrays (structural sharing not
/// required due to typically small cardinality).</para>
/// <para>Thread-safety: Instances are fully immutable; safe for cross-thread reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// var spec = new IncludeSpecification&lt;Order&gt;()
///   .Append(orderCustomerChain)
///   .Append("Lines.Product");
/// var query = context.Orders.Include(spec);
/// </code>
/// </example>
public sealed class IncludeSpecification<T>(
  IEnumerable<IIncludeChain<T>>? chains = null,
  IEnumerable<string>? stringPaths = null)
  : IIncludeSpecification<T> {

  /// <summary>
  /// Gets the strongly typed include chains (never null; may be empty).
  /// </summary>
  public IReadOnlyList<IIncludeChain<T>> Chains { get; } =
    chains is null ? Array.Empty<IIncludeChain<T>>() : [.. chains];

  /// <summary>
  /// Gets the string include paths (never null; may be empty).
  /// </summary>
  public IReadOnlyList<string> StringPaths { get; } =
    stringPaths is null ? Array.Empty<string>() : [.. stringPaths];

  /// <summary>
  /// Creates a new specification with an additional typed include chain appended.
  /// </summary>
  /// <param name="chain">Non-null chain to append.</param>
  /// <returns>New specification instance containing existing and new chain.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="chain"/> is null.</exception>
  public IncludeSpecification<T> Append(IIncludeChain<T> chain) {
    ArgumentNullException.ThrowIfNull(chain);
    return new IncludeSpecification<T>([.. Chains, chain], StringPaths);
  }

  /// <summary>
  /// Creates a new specification with an additional string include path appended.
  /// </summary>
  /// <param name="path">Non-empty provider path (dot notation etc.).</param>
  /// <returns>New specification instance containing existing and new path.</returns>
  /// <exception cref="ArgumentException">When <paramref name="path"/> is null or whitespace.</exception>
  public IncludeSpecification<T> Append(string path) {
    if (string.IsNullOrWhiteSpace(path)) {
      throw new ArgumentException("Path cannot be null or whitespace", nameof(path));
    }
    return new IncludeSpecification<T>(Chains, [.. StringPaths, path]);
  }
}
