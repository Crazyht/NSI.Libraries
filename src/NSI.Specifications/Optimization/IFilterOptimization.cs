using System.Linq.Expressions;
using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Optimization;

/// <summary>
/// Contract for a provider / store specific optimization capable of rewriting a specification into
/// a more efficient predicate (e.g. translating pattern rules into index-friendly forms).
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Advertise target entity via <see cref="EntityType"/>.</description></item>
///   <item><description>Advertise supported specification (open or closed generic) via
///     <see cref="SpecificationType"/>.</description></item>
///   <item><description>Attempt predicate rewrite with <see cref="TryRewriteLambda(object)"/> and return
///     null when not applicable.</description></item>
/// </list>
/// </para>
/// <para>Usage: Implementations are typically discovered via DI and invoked before default
/// specification evaluation. First successful non-null rewrite wins.</para>
/// <para>Performance: Implementations should be side-effect free and quick (pure pattern checks +
/// lightweight expression construction). Avoid reflection in the hot path—cache MethodInfo if
/// needed using static readonly fields per repository standards.</para>
/// <para>Thread-safety: Implementations must be stateless or otherwise thread safe.</para>
/// </remarks>
public interface IFilterOptimization {
  /// <summary>Gets the entity CLR type this optimization targets.</summary>
  /// <value>Concrete entity type (never null).</value>
  public Type EntityType { get; }

  /// <summary>Gets the supported specification type (may be an open generic definition).</summary>
  /// <value>Specification type handled; used for fast eligibility filtering.</value>
  public Type SpecificationType { get; }

  /// <summary>
  /// Attempts to produce an optimized predicate expression for the given specification.
  /// </summary>
  /// <param name="specification">Specification instance (non-null).</param>
  /// <returns>
  /// A rewritten predicate (<c>Expression&lt;Func&lt;TEntity,bool&gt;&gt;</c>) wrapped as a
  /// <see cref="LambdaExpression"/>, or null when the optimization does not apply.
  /// </returns>
  /// <exception cref="ArgumentNullException">When <paramref name="specification"/> is null.</exception>
  public LambdaExpression? TryRewriteLambda(object specification);
}

/// <summary>
/// Strongly-typed convenience base simplifying <see cref="IFilterOptimization"/> implementations
/// by handling type discrimination and exposing a typed override.
/// </summary>
/// <typeparam name="TEntity">Entity type optimized.</typeparam>
/// <typeparam name="TSpec">Specification type supported (must implement
/// <see cref="ISpecification{TEntity}"/>).</typeparam>
/// <remarks>
/// <para>Implements the untyped interface method, performing a safe type check then delegating to
/// <see cref="TryRewrite(TSpec)"/>.</para>
/// <para>Override <see cref="TryRewrite(TSpec)"/> with pattern detection + expression synthesis;
/// return null to signal no optimization.</para>
/// </remarks>
public abstract class FilterOptimization<TEntity, TSpec>: IFilterOptimization
  where TSpec : ISpecification<TEntity> {
  /// <inheritdoc />
  public Type EntityType => typeof(TEntity);

  /// <inheritdoc />
  public Type SpecificationType => typeof(TSpec);

  /// <inheritdoc />
  public LambdaExpression? TryRewriteLambda(object specification) {
    ArgumentNullException.ThrowIfNull(specification);
    return specification is TSpec typed ? TryRewrite(typed) : null;
  }

  /// <summary>
  /// Attempts provider-specific rewrite of the supplied specification into a more efficient
  /// predicate expression.
  /// </summary>
  /// <param name="specification">Typed specification instance (non-null).</param>
  /// <returns>Optimized predicate or null when no improvement is available.</returns>
  public abstract Expression<Func<TEntity, bool>>? TryRewrite(TSpec specification);
}
