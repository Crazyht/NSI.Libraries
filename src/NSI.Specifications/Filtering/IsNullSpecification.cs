using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Matches when the selected (possibly deep) member chain resolves to null (any intermediate null counts as null).
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Member type.</typeparam>
public sealed class IsNullSpecification<T, TKey>(Expression<Func<T, TKey>> selector) : Specification<T>, IFilterSpecification<T> where TKey : class {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  /// <summary>
  /// Builds an expression returning true when any segment of the path or the terminal value is null.
  /// </summary>
  /// <returns>Null detection expression.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    var param = _Selector.Parameters[0];
    var chain = MemberChainExtractor.Extract(_Selector.Body);
    if (chain.Count == 0) {
      return Expression.Lambda<Func<T, bool>>(Expression.Equal(_Selector.Body, Expression.Constant(null, typeof(TKey))), param);
    }
    Expression current = param;
    Expression? orChain = null;
    foreach (var member in chain) {
      current = Expression.MakeMemberAccess(current, member);
      var isNull = Expression.Equal(current, Expression.Constant(null, current.Type));
      orChain = orChain is null ? isNull : Expression.OrElse(orChain, isNull);
    }
    return Expression.Lambda<Func<T, bool>>(orChain!, param);
  }
}
