using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Projection;

/// <summary>
/// Defines a reusable projection from <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TSource">Source entity type.</typeparam>
/// <typeparam name="TResult">Projection result type.</typeparam>
public interface IProjectionSpecification<TSource, TResult> {
  /// <summary>
  /// Gets the selector expression used for the projection.
  /// </summary>
  public Expression<Func<TSource, TResult>> Selector { get; }
}
