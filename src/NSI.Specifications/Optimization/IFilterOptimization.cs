using System;
using System.Linq.Expressions;
using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Optimization;

/// <summary>
/// Contract for a provider-specific optimization able to transform a specification into a better-suited predicate.
/// </summary>
public interface IFilterOptimization {
  /// <summary>
  /// Entity type this optimization targets.
  /// </summary>
  public Type EntityType { get; }

  /// <summary>
  /// Concrete specification type this optimization supports. Can be an open generic definition.
  /// </summary>
  public Type SpecificationType { get; }

  /// <summary>
  /// Attempts to rewrite the predicate. Returns null if not applicable.
  /// </summary>
  /// <param name="specification">Specification instance to rewrite.</param>
  /// <returns>Rewritten predicate, or null to fallback to default behavior.</returns>
  public LambdaExpression? TryRewriteLambda(object specification);
}

/// <summary>
/// Strongly-typed convenience base for filter optimizations.
/// </summary>
public abstract class FilterOptimization<TEntity, TSpec>: IFilterOptimization
  where TSpec : ISpecification<TEntity> {
  /// <inheritdoc />
  public Type EntityType => typeof(TEntity);
  /// <inheritdoc />
  public Type SpecificationType => typeof(TSpec);

  /// <inheritdoc />
  public LambdaExpression? TryRewriteLambda(object specification) {
    if (specification is not TSpec typed) {
      return null;
    }
    var expr = TryRewrite(typed);
    return expr;
  }

  /// <summary>
  /// Provider-specific rewrite. Return null to keep default behavior.
  /// </summary>
  /// <param name="specification">Specification to optimize.</param>
  /// <returns>Optimized predicate, or null.</returns>
  public abstract Expression<Func<TEntity, bool>>? TryRewrite(TSpec specification);
}
