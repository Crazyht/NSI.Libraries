using System.Linq.Expressions;

namespace NSI.Specifications.Include;

/// <summary>
/// Represents a strongly-typed include navigation chain starting at the root entity and
/// proceeding through one or more reference / collection navigation members.
/// </summary>
/// <typeparam name="TRoot">Root entity type from which navigation begins.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>The first lambda in <see cref="Steps"/> is applied with <c>Include</c>.</description></item>
///   <item><description>Each subsequent lambda is applied with <c>ThenInclude</c> in order.</description></item>
///   <item><description>Lambdas are simple member access expressions (no method calls).</description></item>
///   <item><description>Supports both reference and collection navigations (provider dependent).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer concise chains; avoid deep graph loading if partial data suffices.</description></item>
///   <item><description>Group related chains inside an <see cref="IIncludeSpecification{T}"/> implementation.</description></item>
///   <item><description>Do not include scalar properties; they add no eager loading benefit.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Excessive breadth or depth can expand result set (cartesian amplification).</description></item>
///   <item><description>Prefer splitting into multiple focused queries for large object graphs.</description></item>
///   <item><description>Typed lambdas enable compile-time validation and refactoring safety.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations should be immutable; lists exposed are read-only.</para>
/// </remarks>
/// <example>
/// <code>
/// // Example concrete implementation
/// public sealed class OrderCustomerChain: IIncludeChain&lt;Order&gt; {
///   public IReadOnlyList&lt;LambdaExpression&gt; Steps { get; } = new LambdaExpression[] {
///     (Expression&lt;Func&lt;Order, Customer&gt;&gt;)(o =&gt; o.Customer),
///     (Expression&lt;Func&lt;Customer, Address&gt;&gt;)(c =&gt; c.PrimaryAddress)
///   };
/// }
/// </code>
/// </example>
public interface IIncludeChain<TRoot> {
  /// <summary>
  /// Gets the ordered sequence of navigation member access lambdas constituting the chain.
  /// </summary>
  /// <value>
  /// Read-only list where index 0 is the root <c>Include</c> target and each subsequent element
  /// corresponds to a <c>ThenInclude</c> step.
  /// </value>
  public IReadOnlyList<LambdaExpression> Steps { get; }

  /// <summary>
  /// Gets the root entity CLR type (alias for <c>typeof(<typeparamref name="TRoot"/>)</c>).
  /// </summary>
  /// <value>Root entity <see cref="Type"/>.</value>
  public Type RootType => typeof(TRoot);
}
