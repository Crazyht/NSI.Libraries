using System;
using System.Linq;

namespace NSI.Specifications.Sorting;

/// <summary>
/// Extension helpers to apply sorting.
/// </summary>
public static class SortingExtensions
{
    /// <summary>
    /// Applies an <see cref="ISortSpecification{T}"/> to an <see cref="IQueryable{T}"/>.
    /// </summary>
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, ISortSpecification<T>? specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (specification == null || specification.Clauses.Count == 0)
        {
            return source;
        }

        IOrderedQueryable<T>? ordered = null;
        foreach (var clause in specification.Clauses.OrderBy(c => c.OrderIndex))
        {
            var lambda = clause.KeySelector;
            var keyType = lambda.ReturnType;
            if (ordered == null)
            {
                var method = (clause.Direction == SortDirection.Asc ? OrderByMethod : OrderByDescendingMethod).MakeGenericMethod(typeof(T), keyType);
                ordered = (IOrderedQueryable<T>)method.Invoke(null, [source, lambda])!;
            }
            else
            {
                var method = (clause.Direction == SortDirection.Asc ? ThenByMethod : ThenByDescendingMethod).MakeGenericMethod(typeof(T), keyType);
                ordered = (IOrderedQueryable<T>)method.Invoke(null, [ordered, lambda])!;
            }
        }
        return ordered ?? source;
    }

    private static readonly System.Reflection.MethodInfo OrderByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2);
    private static readonly System.Reflection.MethodInfo OrderByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Length == 2);
    private static readonly System.Reflection.MethodInfo ThenByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Length == 2);
    private static readonly System.Reflection.MethodInfo ThenByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.ThenByDescending) && m.GetParameters().Length == 2);
}
