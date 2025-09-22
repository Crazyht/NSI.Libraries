namespace NSI.Specifications.Include;

/// <summary>
/// Defines an aggregate of navigation include chains (typed and string-based) to be applied to
/// queryables supporting eager-loading semantics (e.g. Entity Framework Core).
/// </summary>
/// <typeparam name="T">Root entity type for which includes are defined.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="Chains"/> exposes strongly typed include chains composed of one
///     <c>Include</c> followed by zero or more <c>ThenInclude</c> segments.</description></item>
///   <item><description><see cref="StringPaths"/> exposes raw string include paths (fallback / legacy
///     scenarios, dynamic construction, or provider limitations).</description></item>
///   <item><description>Consumers should apply typed chains first (compile-time safety) then string
///     paths.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer typed chains to enable refactoring safety and analyzer validation.</description></item>
///   <item><description>Use string paths only for conditional / dynamic graph composition where expression
///     building would be overly complex.</description></item>
///   <item><description>Avoid overlapping includes (redundant graph branches) to reduce query bloat.</description></item>
///   <item><description>Group related chains (e.g. read-model projections) into dedicated specification
///     implementations.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Excessive include breadth can lead to cartesian explosion; validate SQL shape in
///     diagnostics.</description></item>
///   <item><description>Typed chains allow upstream optimizers to detect redundant navigations.</description></item>
///   <item><description>String paths incur runtime parsing / validation at provider level.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations should be immutable; lists returned are read-only views.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderGraphInclude: IIncludeSpecification&lt;Order&gt; {
///   public IReadOnlyList&lt;IIncludeChain&lt;Order&gt;&gt; Chains { get; } = new [] {
///     IncludeChain.For&lt;Order&gt;(o =&gt; o.Customer).Then(c =&gt; c.PrimaryAddress),
///     IncludeChain.For&lt;Order&gt;(o =&gt; o.Lines).Then(l =&gt; l.Product)
///   };
///   public IReadOnlyList&lt;string&gt; StringPaths { get; } = Array.Empty&lt;string&gt;();
/// }
///
/// // Usage with EF Core
/// var spec = new OrderGraphInclude();
/// var query = db.Orders.ApplyIncludes(spec); // Extension resolving both typed + string paths
/// </code>
/// </example>
public interface IIncludeSpecification<T> {
  /// <summary>
  /// Gets the collection of strongly typed include chains representing navigation paths rooted at
  /// <typeparamref name="T"/>. Each chain corresponds to an <c>Include</c> plus zero or more
  /// <c>ThenInclude</c> segments.
  /// </summary>
  /// <value>Read-only list of typed include chains; never null (may be empty).</value>
  public IReadOnlyList<IIncludeChain<T>> Chains { get; }

  /// <summary>
  /// Gets the collection of string include paths (legacy / dynamic scenarios) applied after
  /// <see cref="Chains"/>. Each string follows provider-specific syntax (e.g. EF Core dot notation).
  /// </summary>
  /// <value>Read-only list of string include paths; never null (may be empty).</value>
  public IReadOnlyList<string> StringPaths { get; }
}
