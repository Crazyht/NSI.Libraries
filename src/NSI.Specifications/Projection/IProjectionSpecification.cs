using System.Linq.Expressions;

namespace NSI.Specifications.Projection;

/// <summary>
/// Defines a reusable projection from <typeparamref name="TSource"/> to
/// <typeparamref name="TResult"/> encapsulated as an expression tree for translation (e.g. LINQ providers).
/// </summary>
/// <typeparam name="TSource">Source entity type the projection starts from.</typeparam>
/// <typeparam name="TResult">Materialized projection (DTO / anonymous-like) result type.</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Expose a single <see cref="Selector"/> expression for composition.</description></item>
///   <item><description>Enable caching and reuse of complex projection logic.</description></item>
///   <item><description>Support provider translation (must stay side-effect free and pure).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Keep projections free of runtime-only constructs (DateTime.Now, random, IO).</description></item>
///   <item><description>Prefer property / simple method access so ORMs can translate efficiently.</description></item>
///   <item><description>Flatten only what is required to reduce over-fetching.</description></item>
///   <item><description>Compose smaller specs instead of one very large expression when practical.</description></item>
/// </list>
/// </para>
/// <para>Performance: Expression instances should be immutable and can be cached by DI container or
/// static fields. Avoid repeated closure allocations; use static lambdas where feasible.</para>
/// <para>Thread-safety: Implementations must be stateless or expose immutable state.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class UserSummaryProjection: IProjectionSpecification&lt;User, UserSummaryDto&gt; {
///   public Expression&lt;Func&lt;User, UserSummaryDto&gt;&gt; Selector => u => new UserSummaryDto {
///     Id = u.Id,
///     DisplayName = u.FirstName + " " + u.LastName,
///     IsActive = u.Status == UserStatus.Active
///   };
/// }
///
/// // Usage in a repository method
/// var dtoQuery = usersQueryable.Select(new UserSummaryProjection().Selector);
/// var list = await dtoQuery.ToListAsync(ct);
/// </code>
/// </example>
public interface IProjectionSpecification<TSource, TResult> {
  /// <summary>Gets the selector expression used for the projection.</summary>
  /// <value>Pure, side-effect free expression suitable for LINQ provider translation.</value>
  public Expression<Func<TSource, TResult>> Selector { get; }
}
