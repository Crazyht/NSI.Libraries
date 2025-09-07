using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Represents a single sortable key definition.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface ISortClause<T>
{
    /// <summary>Order index (0-based) indicating application sequence.</summary>
    public int OrderIndex { get; }
    /// <summary>Sort direction.</summary>
    public SortDirection Direction { get; }
    /// <summary>Original key selector lambda.</summary>
    public LambdaExpression KeySelector { get; }
    /// <summary>Boxed key selector to guarantee usage of <typeparamref name="T"/> in interface signature.</summary>
    public Expression<Func<T, object?>> BoxedKeySelector { get; }
}
