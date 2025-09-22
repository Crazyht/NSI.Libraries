using System.Linq.Expressions;

namespace NSI.Specifications.Projection;

/// <summary>
/// Immutable projection specification wrapping a selector expression from
/// <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TSource">Source entity type.</typeparam>
/// <typeparam name="TResult">Projected result type (DTO / flat view model).</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Encapsulate an expression tree for reuse and composition.</description></item>
///   <item><description>Provide a single point where complex projection logic is defined and cached.</description></item>
///   <item><description>Remain side‑effect free to enable provider translation (EF Core, LINQ-to-Objects).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Avoid referencing ambient state or non-deterministic APIs (time, random).</description></item>
///   <item><description>Prefer simple member access / conditional logic to maximize translation.</description></item>
///   <item><description>Derive specialized specs from interfaces for better discoverability when needed.</description></item>
/// </list>
/// </para>
/// <para>Performance: The wrapped expression is stored as-is (no cloning). Instances are cheap and
/// can be registered as singletons where appropriate. Consumers should not repeatedly call
/// <c>.Compile()</c> on the same specification—defer compilation to enumerable adapters only.</para>
/// <para>Thread-safety: Fully immutable once constructed and safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Define a projection specification
/// var spec = new ProjectionSpecification&lt;User, UserSummaryDto&gt;(u => new UserSummaryDto {
///   Id = u.Id,
///   DisplayName = u.FirstName + " " + u.LastName,
///   IsActive = u.Status == UserStatus.Active
/// });
///
/// // Apply on IQueryable (translated by provider)
/// var query = context.Users.Select(spec);
///
/// // Apply in-memory
/// var list = userList.Select(spec).ToList();
/// </code>
/// </example>
/// <param name="selector">Non-null pure selector expression.</param>
/// <exception cref="ArgumentNullException">When <paramref name="selector"/> is null.</exception>
public sealed class ProjectionSpecification<TSource, TResult>(
  Expression<Func<TSource, TResult>> selector)
  : IProjectionSpecification<TSource, TResult> {
  /// <inheritdoc />
  public Expression<Func<TSource, TResult>> Selector { get; } =
    selector ?? throw new ArgumentNullException(nameof(selector));
}
