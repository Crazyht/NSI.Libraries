using System.Linq.Expressions;

namespace NSI.Specifications.Include;

/// <summary>
/// Immutable implementation of <see cref="IIncludeChain{TRoot}"/> encapsulating an ordered set of
/// navigation member access lambda expressions (root Include + ThenInclude steps).
/// </summary>
/// <typeparam name="TRoot">Root entity type from which the chain originates.</typeparam>
/// <param name="steps">Ordered navigation lambdas (index 0 => Include, rest => ThenInclude).</param>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="Steps"/>[0] is applied with <c>Include</c>.</description></item>
///   <item><description>Subsequent steps are applied with <c>ThenInclude</c> in sequence.</description></item>
///   <item><description>All expressions are simple member access lambdas (no method calls).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Stores references only; no runtime analysis or reflection traversal.</description></item>
///   <item><description>Enumeration of <see cref="Steps"/> is O(n) where n is chain depth (small).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Instance is immutable; provided list must itself be read-only.</para>
/// </remarks>
internal sealed class IncludeChain<TRoot>(IReadOnlyList<LambdaExpression> steps): IIncludeChain<TRoot> {
  public IReadOnlyList<LambdaExpression> Steps { get; } =
    steps ?? throw new ArgumentNullException(nameof(steps));

  public Type RootType => typeof(TRoot);
}
