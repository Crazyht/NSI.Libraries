using System.Linq.Expressions;
using System.Reflection;
using NSI.Core.Common;
using NSI.Specifications.Core;

namespace NSI.Specifications.Filtering.Text;

/// <summary>
/// Specification filtering entities whose selected string contains a configured search term with
/// optional case-insensitive comparison.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <param name="selector">Projection selecting the string value to test from an entity.</param>
/// <param name="term">Search term to look for within the selected string.</param>
/// <param name="ignoreCase">When true, performs case-insensitive comparison using ToLower().</param>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Evaluates selector(entity) string containment against a fixed term.</description></item>
///   <item><description>Empty or null term yields a predicate that is always false.</description></item>
///   <item><description>Case-insensitive mode uses ordinal ToLower() on both operands.</description></item>
///   <item><description>Null candidate strings never match (explicit null guard).</description></item>
/// </list>
/// </para>
/// <para>
/// EF note: EF Core providers commonly translate ToLower()/Contains(string) to SQL LOWER()/LIKE.
/// Do not use CultureInfo/StringComparison overloads here to preserve translatability.
/// </para>
/// </remarks>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
public sealed class ContainsSpecification<T>(
  Expression<Func<T, string?>> selector,
  string term,
  bool ignoreCase = true): Specification<T>, IFilterSpecification<T> {

  // Analyzer suppressions are scoped to the minimal region.
  // Justification: EF Core translation does not support CultureInfo or
  // StringComparison overloads. Using parameterless ToLower() and
  // Contains(string) keeps queries translatable across providers.
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable S4056  // Use overload that takes IFormatProvider
  private static readonly MethodInfo ToLowerMethod =
    MI.Of<string, string>(s => s.ToLower());
#pragma warning restore S4056
#pragma warning restore CA1311
#pragma warning restore CA1304

#pragma warning disable S4058 // Prefer overload with StringComparison - not supported by EF translation
  private static readonly MethodInfo ContainsMethod =
    MI.Of<string, bool>(s => s.Contains(default(string)!));
#pragma warning restore S4058

  private readonly Expression<Func<T, string?>> _Selector =
    selector ?? throw new ArgumentNullException(nameof(selector));
  private readonly string _Term = term ?? string.Empty;
  private readonly bool _IgnoreCase = ignoreCase;

  /// <summary>
  /// Builds the predicate expression implementing string containment with optional case-insensitivity.
  /// </summary>
  /// <returns>Expression yielding true when the selected string contains the configured term.</returns>
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

    var containsCall = Expression.Call(candidateExpr, ContainsMethod, termExpr);
    var notNull =
      Expression.NotEqual(_Selector.Body, Expression.Constant(null, typeof(string)));
    Expression predicate = Expression.AndAlso(notNull, containsCall);

    if (body is MemberExpression me && me.Expression is not ParameterExpression) {
      predicate = GuardBuilder.Build(_Selector.Body, predicate, parameter);
    }

    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
  }
}
