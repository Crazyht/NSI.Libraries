using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that a non-nullable comparable property lies within an inclusive range.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <typeparam name="TValue">Comparable value type implementing <see cref="IComparable{TValue}"/>.</typeparam>
/// <remarks>
/// <para>
/// Performs an inclusive boundary check [<c>min</c>, <c>max</c>] on a required (non-nullable) value
/// type or reference type implementing <see cref="IComparable{TValue}"/>. A value strictly below
/// the minimum produces an <c>OUT_OF_RANGE_MIN</c> error; a value strictly above the maximum
/// produces an <c>OUT_OF_RANGE_MAX</c> error. Equality with either boundary is accepted.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Single responsibility: range constraint only.</description></item>
///   <item><description>Min &gt; max at construction triggers <see cref="ArgumentOutOfRangeException"/>.</description></item>
///   <item><description>Emits at most one error (value cannot violate both bounds simultaneously).</description></item>
///   <item><description>Never returns <see langword="null"/>; yields zero or one error.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Use a NullableRangeRule when the property is optional.</description></item>
///   <item><description>Keep domain ranges centralized (constants) to avoid divergence.</description></item>
///   <item><description>Prefer domain meaningful min/max (e.g. <c>MinimumAge</c>) over literals.</description></item>
///   <item><description>Combine with other rules (Required, Precision) via validator composition.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Instance is immutable; safe for concurrent reuse.
/// </para>
/// <para>
/// Performance: Two comparisons worst-case; early branch exits eliminate unnecessary operations.
/// Zero allocations when value passes range. Error object allocated only on failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ageRule = new RangeRule&lt;User, int&gt;(u => u.Age, 18, 65);
/// foreach (var e in ageRule.Validate(user, context)) {
///   Console.WriteLine($"{e.PropertyName}: {e.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class RangeRule<T, TValue>: IValidationRule<T>
  where TValue : IComparable<TValue> {
  private readonly Func<T, TValue> _PropertyAccessor;
  private readonly string _PropertyName;
  private readonly TValue _Min;
  private readonly TValue _Max;

  /// <summary>
  /// Creates the rule for the specified comparable property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access (e.g. <c>x => x.Amount</c>).</param>
  /// <param name="min">Inclusive minimum value.</param>
  /// <param name="max">Inclusive maximum value.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> &gt; <paramref name="max"/>.</exception>
  public RangeRule(
    Expression<Func<T, TValue>> propertyExpression,
    TValue min,
    TValue max) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    if (min.CompareTo(max) > 0) {
      throw new ArgumentOutOfRangeException(nameof(min), "Minimum must be less than or equal to maximum.");
    }

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
    _Min = min;
    _Max = max;
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    // Check lower bound first; impossible to violate both simultaneously for valid min/max.
    if (value.CompareTo(_Min) < 0) {
      yield return new ValidationError(
        "OUT_OF_RANGE_MIN",
        $"{_PropertyName} must be at least {_Min}.",
        _PropertyName,
        _Min
      );
      yield break; // Upper bound check unnecessary
    }

    if (value.CompareTo(_Max) > 0) {
      yield return new ValidationError(
        "OUT_OF_RANGE_MAX",
        $"{_PropertyName} must not exceed {_Max}.",
        _PropertyName,
        _Max
      );
    }
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
