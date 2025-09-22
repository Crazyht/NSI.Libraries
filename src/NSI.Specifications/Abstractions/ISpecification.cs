using System.Linq.Expressions;

namespace NSI.Specifications.Abstractions;

/// <summary>
/// Describes a boolean business rule (predicate) over an entity of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity (aggregate / value object) type evaluated.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Represents a pure, side‑effect free predicate.</description></item>
///   <item><description>Provides an expression tree for translation (e.g. LINQ to Entities).</description></item>
///   <item><description>Supports in-memory evaluation when compiled.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Compose specifications via And / Or / Not helpers (see implementing extensions).</description></item>
///   <item><description>Avoid capturing ambient state inside the expression (ensures query provider translation).</description></item>
///   <item><description>Keep predicates simple; delegate complex logic to domain services then map to expressions.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Cache compiled delegates when re-evaluating frequently in memory.</description></item>
///   <item><description>Expression is built once per specification instance (immutable design recommended).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations should be immutable and therefore thread-safe.</para>
/// </remarks>
/// <example>
/// <code>
/// // Example entity
/// public sealed class User {
///   public int Age { get; init; }
///   public bool Active { get; init; }
/// }
///
/// // Concrete specification
/// public sealed class ActiveAdultUserSpecification: ISpecification&lt;User&gt; {
///   private static readonly Expression&lt;Func&lt;User, bool&gt;&gt; Expr = u =&gt; u.Active &amp;&amp; u.Age &gt;= 18;
///   public Expression&lt;Func&lt;User, bool&gt;&gt; ToExpression() => Expr;
///   public bool IsSatisfiedBy(User candidate) => Expr.Compile()(candidate);
/// }
///
/// // Usage (LINQ to Entities or in-memory)
/// var spec = new ActiveAdultUserSpecification();
/// var adults = users.AsQueryable().Where(spec.ToExpression());
/// var isOk = spec.IsSatisfiedBy(new User { Age = 21, Active = true });
/// </code>
/// </example>
public interface ISpecification<T> {
  /// <summary>
  /// Returns the predicate as an <see cref="Expression"/> suitable for LINQ provider translation.
  /// </summary>
  /// <returns>An expression yielding <see langword="true"/> when the candidate satisfies the rule.</returns>
  public Expression<Func<T, bool>> ToExpression();

  /// <summary>
  /// Evaluates the specification against a concrete instance (in-memory predicate invocation).
  /// </summary>
  /// <param name="candidate">Entity instance to test (non-null).</param>
  /// <returns><see langword="true"/> if the candidate satisfies the rule; otherwise <see langword="false"/>.</returns>
  /// <remarks>Implementations typically compile (or reuse a cached compiled) delegate from <see cref="ToExpression"/>.</remarks>
  public bool IsSatisfiedBy(T candidate);
}
