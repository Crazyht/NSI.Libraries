using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Filters entities whose selected value is contained in a fixed set.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Value type.</typeparam>
public sealed class InSpecification<T, TKey>(Expression<Func<T, TKey>> selector, IEnumerable<TKey> values): Specification<T>, IFilterSpecification<T> {
  private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly TKey[] _Values = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
  /// <summary>
  /// Builds the membership predicate expression (null-safe for multi-level paths).
  /// </summary>
  /// <returns>Expression representing membership test.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    if (_Values.Length == 0) {
      return _ => false;
    }
    var valueArray = Expression.Constant(_Values);
    var param = _Selector.Parameters[0];
    var body = _Selector.Body;
    var containsCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TKey)], valueArray, body);
    // Guard multi-level path
    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      var guarded = GuardBuilder.Build(_Selector.Body, containsCall, param);
      return Expression.Lambda<Func<T, bool>>(guarded, param);
    }
    return Expression.Lambda<Func<T, bool>>(containsCall, param);
  }
}
