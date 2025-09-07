using System;
using System.Linq.Expressions;
using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Core;

/// <summary>
/// Combinator and conversion helpers for specifications.
/// </summary>
public static class SpecificationExtensions {
  /// <summary>
  /// Logical AND between two specifications.
  /// </summary>
  public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return new ExpressionSpecification<T>(ExpressionCombiner.And(left.ToExpression(), right.ToExpression()));
  }

  /// <summary>
  /// Logical OR between two specifications.
  /// </summary>
  public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return new ExpressionSpecification<T>(ExpressionCombiner.Or(left.ToExpression(), right.ToExpression()));
  }

  /// <summary>
  /// Logical negation.
  /// </summary>
  public static ISpecification<T> Not<T>(this ISpecification<T> inner) {
    ArgumentNullException.ThrowIfNull(inner);
    return new ExpressionSpecification<T>(ExpressionCombiner.Not(inner.ToExpression()));
  }

  /// <summary>
  /// Converts a raw predicate expression into a specification.
  /// </summary>
  public static ISpecification<T> ToSpecification<T>(this Expression<Func<T, bool>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return new ExpressionSpecification<T>(expression);
  }
}
