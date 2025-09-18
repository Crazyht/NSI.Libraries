using System.Linq.Expressions;

namespace NSI.Specifications.Include;

/// <summary>
/// Factory helpers for creating strongly-typed include chains used by
/// <see cref="IIncludeSpecification{T}"/> implementations.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Validate supplied lambda expressions (non-null, member access bodies).</description></item>
///   <item><description>Ensure at least one step is present (root Include).</description></item>
///   <item><description>Produce immutable <see cref="IIncludeChain{T}"/> instances.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Pass lambdas in navigation order (root to leaf).</description></item>
///   <item><description>Avoid very deep chains; split into multiple specs if necessary.</description></item>
///   <item><description>Only member access expressions are supported (no method calls, conditionals).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Validation is O(n) over number of steps (typically small).</description></item>
///   <item><description>No expression rewriting is performed; lambdas are stored as-is.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Created chains are immutable and safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Build a chain: Order => Customer => PrimaryAddress
/// var chain = IncludeChains.For&lt;Order&gt;(
///   (Expression&lt;Func&lt;Order, Customer&gt;&gt;)(o =&gt; o.Customer),
///   (Expression&lt;Func&lt;Customer, Address&gt;&gt;)(c =&gt; c.PrimaryAddress)
/// );
/// </code>
/// </example>
public static class IncludeChains {
  /// <summary>
  /// Creates an include chain from a sequence of navigation member access expressions.
  /// </summary>
  /// <typeparam name="T">Root entity type.</typeparam>
  /// <param name="steps">Ordered navigation lambdas (root first).</param>
  /// <returns>Immutable include chain instance.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="steps"/> is null.</exception>
  /// <exception cref="ArgumentException">When list is empty or any element invalid.</exception>
  public static IIncludeChain<T> For<T>(params LambdaExpression[] steps) {
    ArgumentNullException.ThrowIfNull(steps);
    if (steps.Length == 0) {
      throw new ArgumentException("At least one step is required", nameof(steps));
    }
    for (var i = 0; i < steps.Length; i++) {
      var s = steps[i] ?? throw new ArgumentException(
        $"Step at index {i} is null", nameof(steps));
      if (s.Body is not MemberExpression) {
        throw new ArgumentException(
          $"Step at index {i} must be a member access lambda (got {s.Body.NodeType}).",
          nameof(steps));
      }
    }
    return new IncludeChain<T>(steps);
  }
}
