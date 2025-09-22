using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Composes boolean predicate <see cref="Expression"/> trees (AND / OR / NOT) with parameter unification.
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Preserves the parameter instance from the left (or inner) expression.</description></item>
///   <item><description>Performs a pure tree transformation (no compilation / invocation).</description></item>
///   <item><description>Generated expressions are suitable for LINQ provider translation.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Guard clauses ensure non-null inputs; callers need not pre-validate.</description></item>
///   <item><description>Avoid capturing mutable external state inside supplied expressions.</description></item>
///   <item><description>Favor higher-level specification combinators to invoke these helpers.</description></item>
/// </list>
/// </para>
/// <para>Performance: Minimal allocation (new combined nodes only). Complexity O(1) aside from
/// parameter replacement traversal in the right branch.</para>
/// <para>Thread-safety: Methods are stateless and safe for concurrent use.</para>
/// </remarks>
internal static class ExpressionCombiner {
  /// <summary>
  /// Combines two predicate expressions with logical AND short-circuit semantics.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="left">Left predicate (its parameter is retained).</param>
  /// <param name="right">Right predicate (its parameter is replaced).</param>
  /// <returns>Expression representing <c>left AND right</c>.</returns>
  /// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null.</exception>
  internal static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    var parameter = left.Parameters[0];
    var rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], parameter);
    return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
  }

  /// <summary>
  /// Combines two predicate expressions with logical OR short-circuit semantics.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="left">Left predicate (its parameter is retained).</param>
  /// <param name="right">Right predicate (its parameter is replaced).</param>
  /// <returns>Expression representing <c>left OR right</c>.</returns>
  /// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null.</exception>
  internal static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    var parameter = left.Parameters[0];
    var rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], parameter);
    return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightBody), parameter);
  }

  /// <summary>
  /// Negates a predicate expression while preserving its parameter reference.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="inner">Predicate to negate.</param>
  /// <returns>Expression representing <c>NOT inner</c>.</returns>
  /// <exception cref="ArgumentNullException">If <paramref name="inner"/> is null.</exception>
  internal static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> inner) {
    ArgumentNullException.ThrowIfNull(inner);
    var parameter = inner.Parameters[0];
    return Expression.Lambda<Func<T, bool>>(Expression.Not(inner.Body), parameter);
  }
}
