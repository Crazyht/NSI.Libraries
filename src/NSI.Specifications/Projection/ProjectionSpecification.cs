using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Projection;

/// <summary>
/// Immutable projection specification wrapping a selector expression.
/// </summary>
/// <typeparam name="TSource">Source entity type.</typeparam>
/// <typeparam name="TResult">Result type.</typeparam>
public sealed class ProjectionSpecification<TSource, TResult>(Expression<Func<TSource, TResult>> selector): IProjectionSpecification<TSource, TResult> {
  /// <inheritdoc />
  public Expression<Func<TSource, TResult>> Selector { get; } = selector ?? throw new ArgumentNullException(nameof(selector));
}
