using System;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Filters entities whose selected value lies between provided bounds.
/// </summary>
public sealed class BetweenSpecification<T, TKey>(
  Expression<Func<T, TKey>> selector,
  TKey lower,
  TKey upper,
  bool includeLower = true,
  bool includeUpper = true)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey>
{
    private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
    private readonly TKey _Lower = lower;
    private readonly TKey _Upper = upper;
    private readonly bool _IncludeLower = includeLower;
    private readonly bool _IncludeUpper = includeUpper;

    /// <inheritdoc />
    public override Expression<Func<T, bool>> ToExpression()
    {
        var parameter = _Selector.Parameters[0];
        var body = _Selector.Body;
        var lowerConst = Expression.Constant(_Lower, typeof(TKey));
        var upperConst = Expression.Constant(_Upper, typeof(TKey));

        Expression lowerExpr = _IncludeLower
          ? Expression.GreaterThanOrEqual(body, lowerConst)
          : Expression.GreaterThan(body, lowerConst);

        Expression upperExpr = _IncludeUpper
          ? Expression.LessThanOrEqual(body, upperConst)
          : Expression.LessThan(body, upperConst);

        Expression combined = Expression.AndAlso(lowerExpr, upperExpr);

        if (body is MemberExpression me && me.Expression is not ParameterExpression)
        {
            combined = GuardBuilder.Build(_Selector.Body, combined, parameter);
        }
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}
