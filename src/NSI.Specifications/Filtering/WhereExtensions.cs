using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NSI.Specifications.Abstractions;
using NSI.Specifications.Optimization;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Applies specifications to queryables and enumerables, leveraging provider-specific optimizations when available.
/// </summary>
public static class WhereExtensions
{
    /// <summary>
    /// Combines two specifications with logical AND.
    /// </summary>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return InlineSpec<T>.Create(left, right, static (l, r) => Expression.AndAlso(l, r));
    }

    /// <summary>
    /// Combines two specifications with logical OR.
    /// </summary>
    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return InlineSpec<T>.Create(left, right, static (l, r) => Expression.OrElse(l, r));
    }

    /// <summary>
    /// Negates a specification.
    /// </summary>
    public static ISpecification<T> Not<T>(this ISpecification<T> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return InlineSpec<T>.Create(spec, static e => Expression.Not(e));
    }
    /// <summary>
    /// Applies a specification to IQueryable with optional provider-specific optimization.
    /// </summary>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        var provider = ProviderNameResolver.Resolve(source);
        var candidates = SpecOptimizationRegistry.Get(provider, specification.GetType());
        foreach (var opt in candidates)
        {
            var lambda = opt.TryRewriteLambda(specification);
            if (lambda is Expression<Func<T, bool>> typed)
            {
                return source.Where(typed);
            }
        }

        return source.Where(specification.ToExpression());
    }

    /// <summary>
    /// Applies a specification to in-memory enumerable (no optimization needed).
    /// </summary>
    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        var predicate = specification.ToExpression().Compile();
        return source.Where(predicate);
    }
}

file sealed class InlineSpec<T> : ISpecification<T>
{
    private readonly Expression<Func<T, bool>> _Expression;
    private InlineSpec(Expression<Func<T, bool>> expression) => _Expression = expression;
    public static ISpecification<T> Create(ISpecification<T> left, ISpecification<T> right, Func<Expression, Expression, Expression> merge)
    {
        var l = left.ToExpression();
        var r = right.ToExpression();
        var param = Expression.Parameter(typeof(T), "x");
        var replacedL = new ReplaceVisitor(l.Parameters[0], param).Visit(l.Body)!;
        var replacedR = new ReplaceVisitor(r.Parameters[0], param).Visit(r.Body)!;
        var body = merge(replacedL, replacedR);
        return new InlineSpec<T>(Expression.Lambda<Func<T, bool>>(body, param));
    }
    public static ISpecification<T> Create(ISpecification<T> inner, Func<Expression, Expression> transform)
    {
        var e = inner.ToExpression();
        var param = Expression.Parameter(typeof(T), "x");
        var replaced = new ReplaceVisitor(e.Parameters[0], param).Visit(e.Body)!;
        var body = transform(replaced);
        return new InlineSpec<T>(Expression.Lambda<Func<T, bool>>(body, param));
    }
    public Expression<Func<T, bool>> ToExpression() => _Expression;
    public bool IsSatisfiedBy(T candidate) => _Expression.Compile()(candidate);

    private sealed class ReplaceVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
