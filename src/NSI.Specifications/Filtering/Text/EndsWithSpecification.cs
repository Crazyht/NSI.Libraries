using System;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Text;

/// <summary>
/// Filters entities whose selected string ends with the given term.
/// </summary>
public sealed class EndsWithSpecification<T>(Expression<Func<T, string?>> selector, string term, bool ignoreCase = true)
  : Specification<T>, IFilterSpecification<T> {
  private readonly Expression<Func<T, string?>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly string _Term = term ?? string.Empty;
  private readonly bool _IgnoreCase = ignoreCase;

  /// <inheritdoc />
  public override Expression<Func<T, bool>> ToExpression() {
    if (string.IsNullOrEmpty(_Term)) {
      return _ => false;
    }
    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;

    var stringExpr = body;
    Expression termExpr = Expression.Constant(_Term, typeof(string));

    if (_IgnoreCase) {
      stringExpr = Expression.Call(stringExpr, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
      termExpr = Expression.Call(termExpr, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
    }

    var endsWith = Expression.Call(stringExpr, typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!, termExpr);
    var notNull = Expression.NotEqual(_Selector.Body, Expression.Constant(null, typeof(string)));
    Expression predicate = Expression.AndAlso(notNull, endsWith);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, parameter);
    }
    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
  }
}
