using System;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Filters entities whose selected value equals the provided value.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Value type.</typeparam>
public sealed class EqualsSpecification<T, TKey>(Expression<Func<T, TKey>> selector, TKey value): Specification<T>, IFilterSpecification<T> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey _Value = value;
  /// <summary>
  /// Builds the equality predicate expression (null-safe for multi-level paths).
  /// </summary>
  /// <returns>Expression representing the equality comparison.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    // Build member access (no extra parameter replace needed)
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;
    var constant = Expression.Constant(_Value, typeof(TKey));
    // For reference / nullable types add null guard chain except when comparing to null constant not needed for single-level
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      body = Expression.Equal(body, constant);
      var guarded = GuardBuilder.Build(_Selector.Body, body, parameter);
      return Expression.Lambda<Func<T, bool>>(guarded, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(Expression.Equal(body, constant), parameter);
  }
}

internal static class GuardBuilder {
  public static Expression Build(Expression pathBody, Expression predicate, ParameterExpression parameter) {
    var chain = MemberChainExtractor.Extract(pathBody);
    if (chain.Count == 0) {
      return predicate;
    }
    var current = (Expression)parameter;
    Expression? guard = null;
    foreach (var member in chain) {
      current = Expression.MakeMemberAccess(current, member);
      var memberType = member.GetMemberType();
      if (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null) {
        var notNull = Expression.NotEqual(current, Expression.Constant(null, current.Type));
        guard = guard is null ? notNull : Expression.AndAlso(guard, notNull);
      }
    }
    return guard is null ? predicate : Expression.AndAlso(guard, predicate);
  }
}
