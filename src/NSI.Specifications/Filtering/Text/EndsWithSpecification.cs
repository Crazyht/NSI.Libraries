using System.Linq.Expressions;
using System.Reflection;
using NSI.Core.Common;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Text;

/// <summary>
/// Specification filtering entities whose selected string ends with a configured term with optional
/// case-insensitive comparison.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>EF note: Use ToLower() + EndsWith(string) so EF Core can translate to SQL LOWER()/LIKE.</para>
/// </remarks>
public sealed class EndsWithSpecification<T>(
  Expression<Func<T, string?>> selector,
  string term,
  bool ignoreCase = true)
  : Specification<T>, IFilterSpecification<T> {

#pragma warning disable CA1304, CA1311, S4056 // EF translation compatibility (ToLower())
  private static readonly MethodInfo ToLowerMethod = MI.Of<string, string>(s => s.ToLower());
#pragma warning restore CA1304, CA1311, S4056
#pragma warning disable S4058 // Prefer overload with StringComparison - not supported by EF translation
  private static readonly MethodInfo EndsWithMethod = MI.Of<string, bool>(s => s.EndsWith(default(string)!));
#pragma warning restore S4058

  private readonly Expression<Func<T, string?>> _Selector = selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly string _Term = term ?? string.Empty;
  private readonly bool _IgnoreCase = ignoreCase;

  /// <summary>
  /// Builds the predicate expression implementing suffix comparison with optional case-insensitivity.
  /// </summary>
  /// <returns>Expression yielding true when the selected string ends with the configured term.</returns>
  public override Expression<Func<T, bool>> ToExpression() {
    if (string.IsNullOrEmpty(_Term)) {
      return _ => false;
    }

    var parameter = _Selector.Parameters[0];
    var body = _Selector.Body;

    var candidateExpr = body;
    Expression termExpr = Expression.Constant(_Term, typeof(string));

    if (_IgnoreCase) {
      candidateExpr = Expression.Call(candidateExpr, ToLowerMethod);
      termExpr = Expression.Call(termExpr, ToLowerMethod);
    }

    var endsWith = Expression.Call(candidateExpr, EndsWithMethod, termExpr);
    var notNull = Expression.NotEqual(_Selector.Body, Expression.Constant(null, typeof(string)));
    Expression predicate = Expression.AndAlso(notNull, endsWith);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, parameter);
    }

    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
  }
}
