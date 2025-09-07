using System;
using System.Linq.Expressions;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Comparison;

/// <summary>
/// Filters entities whose selected value is greater than or equal to the provided value.
/// </summary>
public sealed class GreaterThanOrEqualSpecification<T, TKey>(Expression<Func<T, TKey>> selector, TKey value)
  : Specification<T>, IFilterSpecification<T> where TKey : IComparable<TKey>
{
    private readonly Expression<Func<T, TKey>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
    private readonly TKey _Value = value;

    /// <inheritdoc />
    public override Expression<Func<T, bool>> ToExpression()
    {
        var parameter = _Selector.Parameters[0];
        var body = _Selector.Body;
        var constant = Expression.Constant(_Value, typeof(TKey));
        Expression comparison = Expression.GreaterThanOrEqual(body, constant);
        if (body is MemberExpression me && me.Expression is not ParameterExpression)
        {
            comparison = GuardBuilder.Build(_Selector.Body, comparison, parameter);
        }
        return Expression.Lambda<Func<T, bool>>(comparison, parameter);
    }
}
