using System.Linq.Expressions;
using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Core;

/// <summary>
/// Base abstraction providing common infrastructure for concrete <see cref="ISpecification{T}"/>
/// implementations (composition helpers reside in extension classes).
/// </summary>
/// <typeparam name="T">Entity / aggregate root / value object type evaluated.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Defines the contract for a pure boolean rule over <typeparamref name="T"/>.</description></item>
///   <item><description>Exposes an expression tree for provider translation (e.g. EF Core).</description></item>
///   <item><description>Allows in‑memory evaluation through <see cref="IsSatisfiedBy"/>.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Keep <see cref="ToExpression"/> side‑effect free and provider friendly (no uncontrolled closures).</description></item>
///   <item><description>Prefer immutable specification instances (cache internal expressions if expensive).</description></item>
///   <item><description>Use dedicated combiners / extension methods for AND / OR / NOT composition.</description></item>
///   <item><description>Avoid compiling repeatedly in hot paths; callers can cache compiled delegates.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description><see cref="IsSatisfiedBy"/> performs a transient compile; heavy reuse should precompile externally.</description></item>
///   <item><description>Expression trees should minimize redundant operations to aid query provider optimization.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations should be immutable (recommended). This base type is stateless.</para>
/// </remarks>
/// <example>
/// <code>
/// // Simple concrete specification
/// public sealed class ActiveAdultUserSpecification: Specification&lt;User&gt; {
///   private static readonly Expression&lt;Func&lt;User, bool&gt;&gt; Predicate =
///     u => u.Active &amp;&amp; u.Age &gt;= 18;
///   public override Expression&lt;Func&lt;User, bool&gt;&gt; ToExpression() => Predicate;
/// }
///
/// // Usage in a LINQ query
/// var spec = new ActiveAdultUserSpecification();
/// var adults = users.AsQueryable().Where(spec.ToExpression());
/// var inMemoryCheck = spec.IsSatisfiedBy(new User { Active = true, Age = 25 });
/// </code>
/// </example>
public abstract class Specification<T>: ISpecification<T> {
  /// <summary>
  /// Returns the pure predicate expression representing the rule.
  /// </summary>
  /// <returns>Expression returning <see langword="true"/> when the candidate satisfies the rule.</returns>
  public abstract Expression<Func<T, bool>> ToExpression();

  /// <summary>
  /// Evaluates the specification against a provided instance (in‑memory evaluation path).
  /// </summary>
  /// <param name="candidate">Instance to test (non-null).</param>
  /// <returns><see langword="true"/> when the predicate holds.</returns>
  /// <remarks>Compilation is not cached here; callers with repeated evaluation should cache the compiled delegate.</remarks>
  public bool IsSatisfiedBy(T candidate) => ToExpression().Compile()(candidate);
}
