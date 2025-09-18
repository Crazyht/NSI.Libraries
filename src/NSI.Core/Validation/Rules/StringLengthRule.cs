using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that a string property's length lies within inclusive [min, max] boundaries.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <remarks>
/// <para>
/// Applies length constraints only when the target string is non-null. A null value is treated as
/// success so this rule can be composed with <see cref="RequiredRule{T}"/> when presence is
/// mandatory. Both <c>minLength</c> and <c>maxLength</c> are inclusive.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null value => success (no errors).</description></item>
///   <item><description>Length &lt; min => emits <c>TOO_SHORT</c>.</description></item>
///   <item><description>Length &gt; max => emits <c>TOO_LONG</c>.</description></item>
///   <item><description>0 ≤ min ≤ max enforced at construction; otherwise throws.</description></item>
///   <item><description>At most one error emitted (cannot be simultaneously too short and too long).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Combine with <see cref="RequiredRule{T}"/> to disallow null.</description></item>
///   <item><description>Use domain constants for shared limits (e.g. <c>MaxNameLength</c>).</description></item>
///   <item><description>Prefer tighter bounds for user inputs to mitigate abuse.</description></item>
///   <item><description>Localize only the message externally; keep stable error codes.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Immutable after construction; safe for concurrent reuse across threads.
/// </para>
/// <para>
/// Performance: Single delegate invocation + one length retrieval; success path allocates nothing.
/// Early short-circuit prevents unnecessary upper bound comparison.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new StringLengthRule&lt;User&gt;(u => u.Username, 3, 50);
/// foreach (var e in rule.Validate(user, context)) {
///   Console.WriteLine($"{e.PropertyName}: {e.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class StringLengthRule<T>: IValidationRule<T> {
  private readonly Func<T, string?> _PropertyAccessor;
  private readonly string _PropertyName;
  private readonly int _MinLength;
  private readonly int _MaxLength;

  /// <summary>
  /// Creates the rule for the specified string property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access expression (e.g. <c>x => x.Name</c>).</param>
  /// <param name="minLength">Inclusive lower bound (default 0).</param>
  /// <param name="maxLength">Inclusive upper bound (default <see cref="int.MaxValue"/>).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when bounds are invalid (negative min or max &lt; min).</exception>
  public StringLengthRule(
    Expression<Func<T, string?>> propertyExpression,
    int minLength = 0,
    int maxLength = int.MaxValue) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentOutOfRangeException.ThrowIfNegative(minLength);
    ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
    _MinLength = minLength;
    _MaxLength = maxLength;
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);
    if (value is null) {
      yield break; // Null treated as valid; compose with RequiredRule if needed
    }

    var length = value.Length;

    if (length < _MinLength) {
      yield return new ValidationError(
        "TOO_SHORT",
        $"{_PropertyName} must be at least {_MinLength} characters long.",
        _PropertyName,
        _MinLength
      );
      yield break; // Cannot also be too long
    }

    if (length > _MaxLength) {
      yield return new ValidationError(
        "TOO_LONG",
        $"{_PropertyName} must not exceed {_MaxLength} characters.",
        _PropertyName,
        _MaxLength
      );
    }
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
