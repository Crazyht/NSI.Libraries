using System;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Matches when the selected string or collection is empty (null is NOT treated as empty).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public sealed class IsEmptySpecification<T>(Expression<Func<T, object?>> selector) : Specification<T>, IFilterSpecification<T> {
  private readonly Expression<Func<T, object?>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  /// <summary>
  /// Builds an expression that matches empty strings or collections (null excluded).
  /// </summary>
  /// <returns>Expression representing emptiness check.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    var param = _Selector.Parameters[0];
    var body = _Selector.Body;
    // Strip boxing for value types inside object conversion
    if (body is UnaryExpression u && u.NodeType == ExpressionType.Convert) {
      body = u.Operand;
    }
    Expression predicate;
    if (body.Type == typeof(string)) {
      // s != null && s.Length == 0
      var notNull = Expression.NotEqual(body, Expression.Constant(null, body.Type));
      var lengthProp = Expression.Property(body, nameof(string.Length));
      var isEmpty = Expression.Equal(lengthProp, Expression.Constant(0));
      predicate = Expression.AndAlso(notNull, isEmpty);
    } else if (typeof(System.Collections.ICollection).IsAssignableFrom(body.Type)) {
      var notNull = Expression.NotEqual(body, Expression.Constant(null, body.Type));
      var countProp = Expression.Property(body, "Count");
      var isZero = Expression.Equal(countProp, Expression.Constant(0));
      predicate = Expression.AndAlso(notNull, isZero);
    } else {
      throw new NotSupportedException("IsEmptySpecification only supports string or ICollection types.");
    }
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, param);
    }
    return Expression.Lambda<Func<T, bool>>(predicate, param);
  }
}
