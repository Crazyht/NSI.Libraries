using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that an optional (nullable) comparable value lies within an inclusive range when present.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <typeparam name="TValue">Underlying value type (struct) implementing <see cref="IComparable{TValue}"/>.</typeparam>
/// <remarks>
/// <para>
/// Applies an inclusive [<c>min</c>, <c>max</c>] boundary check only when the target nullable
/// property has a value. A missing (null) value is treated as success so this rule can be composed
/// with a separate Required rule when non-null enforcement is desired.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null value => passes (no errors).</description></item>
///   <item><description>Value &lt; min => emits <c>OUT_OF_RANGE_MIN</c>.</description></item>
///   <item><description>Value &gt; max => emits <c>OUT_OF_RANGE_MAX</c>.</description></item>
///   <item><description>Boundaries are inclusive (value == min or value == max is valid).</description></item>
///   <item><description>Never throws for normal validation failures (errors are yielded).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Compose with a Required rule if absence must fail validation.</description></item>
///   <item><description>Use meaningful ranges aligned with domain invariants (avoid magic numbers).</description></item>
///   <item><description>Prefer domain-specific rule names for complex conditional ranges.</description></item>
///   <item><description>Keep <c>min</c>/<c>max</c> stable for backward compatibility in clients.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Immutable after construction; safe for concurrent reuse across threads.
/// </para>
/// <para>
/// Performance: Single delegate invocation + at most two integer/struct comparisons; no allocation
/// on success path. Error objects allocated only when bounds violated.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ageRule = new NullableRangeRule&lt;User, int&gt;(u => u.OptionalAge, 18, 65);
/// foreach (var e in ageRule.Validate(user, context)) {
///   Console.WriteLine($"{e.PropertyName}: {e.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class NullableRangeRule<T, TValue>: IValidationRule<T>
  where TValue : struct, IComparable<TValue> {
  private readonly Func<T, TValue?> _PropertyAccessor;
  private readonly string _PropertyName;
  private readonly TValue _Min;
  private readonly TValue _Max;

  /// <summary>
  /// Creates the rule for the specified nullable comparable property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access (e.g. <c>x => x.OptionalValue</c>).</param>
  /// <param name="min">Inclusive minimum allowed value.</param>
  /// <param name="max">Inclusive maximum allowed value.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
  public NullableRangeRule(
    Expression<Func<T, TValue?>> propertyExpression,
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

    var nullableValue = _PropertyAccessor(instance);
    if (!nullableValue.HasValue) {
      yield break; // Null -> treat as valid
    }

    var value = nullableValue.Value;

    if (value.CompareTo(_Min) < 0) {
      yield return new ValidationError(
        "OUT_OF_RANGE_MIN",
        $"{_PropertyName} must be at least {_Min}.",
        _PropertyName,
        _Min
      );
      // Do not 'return' to allow dual errors if both bounds logically fail (defensive symmetry)
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
