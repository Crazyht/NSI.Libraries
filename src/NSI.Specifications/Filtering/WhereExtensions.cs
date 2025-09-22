using System.Linq.Expressions;
using NSI.Specifications.Abstractions;
using NSI.Specifications.Optimization;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Extension helpers applying and composing <see cref="ISpecification{T}"/> instances for both
/// query provider (IQueryable) and in-memory (IEnumerable) scenarios with optional
/// provider-specific optimization rewrites.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Logical composition of specifications (And / Or / Not).</description></item>
///   <item><description>Translation of composed specifications into a single lambda expression.</description></item>
///   <item><description>Provider optimization hook: attempts registered rewrites before default.</description></item>
///   <item><description>In-memory application via compiled predicates.</description></item>
/// </list>
/// </para>
/// <para>Design Notes:
/// <list type="bullet">
///   <item><description>No reflection; parameter unification via expression visitor.</description></item>
///   <item><description>Optimization registry keyed by provider name + specification type.</description></item>
///   <item><description>Fail-safe: if no optimization matches, original expression is used.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Logical composition O(depth) over expression size.</description></item>
///   <item><description>Inline specification caches compiled delegate (Lazy).</description></item>
///   <item><description>Rewrite loop short-circuits at first successful candidate.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Produced specification instances are immutable.</para>
/// </remarks>
/// <example>
/// <code>
/// // Base specifications
/// ISpecification&lt;User&gt; active = new ActiveUserSpecification();
/// ISpecification&lt;User&gt; adult = new MinimumAgeSpecification(18);
///
/// // Composition
/// var activeAdult = active.And(adult);
///
/// // IQueryable usage (EF Core etc.)
/// var query = context.Users.Where(activeAdult.ToExpression());
///
/// // In-memory usage
/// var filtered = users.Where(activeAdult);
/// </code>
/// </example>
public static class WhereExtensions {
  /// <summary>
  /// Creates a composite specification that succeeds only when both input specifications succeed.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="left">Left-hand specification (non-null).</param>
  /// <param name="right">Right-hand specification (non-null).</param>
  /// <returns>Composite specification representing logical conjunction.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or
  /// <paramref name="right"/> is null.</exception>
  public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return InlineSpec<T>.Create(left, right, static (l, r) => Expression.AndAlso(l, r));
  }

  /// <summary>
  /// Creates a composite specification that succeeds when either input specification succeeds.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="left">Left-hand specification (non-null).</param>
  /// <param name="right">Right-hand specification (non-null).</param>
  /// <returns>Composite specification representing logical disjunction.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or
  /// <paramref name="right"/> is null.</exception>
  public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return InlineSpec<T>.Create(left, right, static (l, r) => Expression.OrElse(l, r));
  }

  /// <summary>
  /// Creates a specification that is satisfied when the original specification is not satisfied.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="spec">Underlying specification (non-null).</param>
  /// <returns>Negated specification.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="spec"/> is null.</exception>
  public static ISpecification<T> Not<T>(this ISpecification<T> spec) {
    ArgumentNullException.ThrowIfNull(spec);
    return InlineSpec<T>.Create(spec, static e => Expression.Not(e));
  }

  /// <summary>
  /// Applies a specification to an <see cref="IQueryable{T}"/>, attempting provider-specific
  /// rewrite optimizations before fallback.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="source">Queryable data source (non-null).</param>
  /// <param name="specification">Specification to apply (non-null).</param>
  /// <returns>Queryable with predicate applied.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or
  /// <paramref name="specification"/> is null.</exception>
  /// <remarks>
  /// <para>Rewrite Process:
  /// <list type="number">
  ///   <item><description>Resolve provider name via <see cref="ProviderNameResolver"/>.</description></item>
  ///   <item><description>Retrieve candidates from <see cref="SpecOptimizationRegistry"/>.</description></item>
  ///   <item><description>Try each optimizer until replacement lambda produced.</description></item>
  ///   <item><description>Fallback: use <see cref="ISpecification{T}.ToExpression"/> output.</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public static IQueryable<T> Where<T>(this IQueryable<T> source, ISpecification<T> specification) {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(specification);

    var provider = ProviderNameResolver.Resolve(source);
    var candidates = SpecOptimizationRegistry.Get(provider, specification.GetType());
    foreach (var opt in candidates) {
      var lambda = opt.TryRewriteLambda(specification);
      if (lambda is Expression<Func<T, bool>> typed) {
        return source.Where(typed);
      }
    }
    return source.Where(specification.ToExpression());
  }

  /// <summary>
  /// Applies a specification to an in-memory sequence using a compiled predicate.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="source">In-memory sequence (non-null).</param>
  /// <param name="specification">Specification to evaluate (non-null).</param>
  /// <returns>Filtered enumerable (deferred execution).</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or
  /// <paramref name="specification"/> is null.</exception>
  /// <remarks>Compilation occurs once per specification instance (implementation dependent).</remarks>
  public static IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> specification) {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(specification);
    var predicate = specification.ToExpression().Compile();
    return source.Where(predicate);
  }
}

/// <summary>
/// Internal inline specification used for logical composition and transformation of specification
/// expressions.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>Merges / transforms expression trees while unifying parameter usage to a single synthetic
/// parameter.</para>
/// <para>Compiled delegate cached to avoid repeated <see cref="LambdaExpression.Compile()"/> cost.</para>
/// </remarks>
file sealed class InlineSpec<T>: ISpecification<T> {
  private readonly Expression<Func<T, bool>> _Expression;
  private readonly Lazy<Func<T, bool>> _Compiled;

  private InlineSpec(Expression<Func<T, bool>> expression) {
    _Expression = expression;
    _Compiled = new Lazy<Func<T, bool>>(() => _Expression.Compile(), true);
  }

  /// <summary>
  /// Creates a composite specification from two specifications using a merge function.
  /// </summary>
  public static ISpecification<T> Create(
    ISpecification<T> left,
    ISpecification<T> right,
    Func<Expression, Expression, Expression> merge) {
    var l = left.ToExpression();
    var r = right.ToExpression();
    var param = Expression.Parameter(typeof(T), "x");
    var replacedL = new ReplaceVisitor(l.Parameters[0], param).Visit(l.Body)!;
    var replacedR = new ReplaceVisitor(r.Parameters[0], param).Visit(r.Body)!;
    var body = merge(replacedL, replacedR);
    return new InlineSpec<T>(Expression.Lambda<Func<T, bool>>(body, param));
  }

  /// <summary>
  /// Creates a derived specification by transforming the existing specification body.
  /// </summary>
  public static ISpecification<T> Create(
    ISpecification<T> inner,
    Func<Expression, Expression> transform) {
    var e = inner.ToExpression();
    var param = Expression.Parameter(typeof(T), "x");
    var replaced = new ReplaceVisitor(e.Parameters[0], param).Visit(e.Body)!;
    var body = transform(replaced);
    return new InlineSpec<T>(Expression.Lambda<Func<T, bool>>(body, param));
  }

  /// <inheritdoc />
  public Expression<Func<T, bool>> ToExpression() => _Expression;

  /// <inheritdoc />
  public bool IsSatisfiedBy(T candidate) => _Compiled.Value(candidate);

  /// <summary>
  /// Visitor replacing a source parameter with a unified target parameter.
  /// </summary>
  private sealed class ReplaceVisitor(ParameterExpression from, ParameterExpression to): ExpressionVisitor {
    protected override Expression VisitParameter(ParameterExpression node)
      => node == from ? to : base.VisitParameter(node);
  }
}
