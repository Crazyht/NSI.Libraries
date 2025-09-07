using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

internal sealed class SortClause<T, TKey>: ISortClause<T> {
  public int OrderIndex { get; }
  public SortDirection Direction { get; }
  public LambdaExpression KeySelector { get; }
  public Expression<Func<T, object?>> BoxedKeySelector { get; }

  public SortClause(int orderIndex, SortDirection direction, Expression<Func<T, TKey>> keySelector) {
    ArgumentNullException.ThrowIfNull(keySelector);
    OrderIndex = orderIndex;
    Direction = direction;
    KeySelector = keySelector;
    BoxedKeySelector = Expression.Lambda<Func<T, object?>>(Expression.Convert(keySelector.Body, typeof(object)), keySelector.Parameters);
  }
}
