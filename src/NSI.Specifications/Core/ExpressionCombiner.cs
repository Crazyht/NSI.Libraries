using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Helpers to compose boolean expressions with parameter unification.
/// </summary>
internal static class ExpressionCombiner {
  public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    var parameter = left.Parameters[0];
    var rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], parameter);
    return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
  }

  public static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    var parameter = left.Parameters[0];
    var rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], parameter);
    return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightBody), parameter);
  }

  public static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> inner) {
    ArgumentNullException.ThrowIfNull(inner);
    var parameter = inner.Parameters[0];
    return Expression.Lambda<Func<T, bool>>(Expression.Not(inner.Body), parameter);
  }
}
