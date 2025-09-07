using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules {
  /// <summary>
  /// Validates that a nullable numeric property value is within a specified range when it has a value.
  /// </summary>
  /// <typeparam name="T">The type of object being validated.</typeparam>
  /// <typeparam name="TValue">The underlying numeric type of the nullable property.</typeparam>
  /// <remarks>
  /// <para>
  /// This validation rule checks that a nullable property value falls within the inclusive
  /// range between the specified minimum and maximum values when the property has a value.
  /// Null values are considered valid and pass validation.
  /// </para>
  /// <para>
  /// The rule works with any nullable type that implements <see cref="IComparable{T}"/>,
  /// making it suitable for various nullable numeric types (int?, decimal?, double?) as well as
  /// other comparable nullable types like DateTime?.
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// // Optional age must be between 18 and 65 when provided
  /// var ageRule = new NullableRangeRule&lt;User, int&gt;(u => u.OptionalAge, 18, 65);
  /// var errors = ageRule.Validate(user, validationContext);
  /// 
  /// // Optional price must be between allowed bounds when provided
  /// var priceRule = new NullableRangeRule&lt;Product, decimal&gt;(p => p.OptionalPrice, 0.01m, 9999.99m);
  /// 
  /// // Optional date must be in the future when provided
  /// var dateRule = new NullableRangeRule&lt;Event, DateTime&gt;(e => e.OptionalDate, DateTime.Today.AddDays(1), DateTime.Today.AddYears(1));
  /// </code>
  /// </para>
  /// </remarks>
  public sealed class NullableRangeRule<T, TValue>: IValidationRule<T>
    where TValue : struct, IComparable<TValue> {
    private readonly Func<T, TValue?> _PropertyAccessor;
    private readonly string _PropertyName;
    private readonly TValue _Min;
    private readonly TValue _Max;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableRangeRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="propertyExpression">Expression to access the nullable property.</param>
    /// <param name="min">Minimum allowed value (inclusive).</param>
    /// <param name="max">Maximum allowed value (inclusive).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertyExpression"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when min is greater than max.
    /// </exception>
    /// <remarks>
    /// The property expression should be a simple member access expression
    /// (e.g., <c>x => x.NullablePropertyName</c>) that returns a nullable comparable value.
    /// </remarks>
    public NullableRangeRule(
      Expression<Func<T, TValue?>> propertyExpression,
      TValue min,
      TValue max) {
      ArgumentNullException.ThrowIfNull(propertyExpression);

      if (min.CompareTo(max) > 0) {
        throw new ArgumentException("Min value must be less than or equal to max value.");
      }

      _PropertyAccessor = propertyExpression.Compile();
      _PropertyName = GetPropertyName(propertyExpression);
      _Min = min;
      _Max = max;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method validates that the nullable property value is within the range specified
    /// during initialization when the property has a value. It returns validation errors when:
    /// <list type="bullet">
    ///   <item><description>The value is not null and is less than the minimum allowed value</description></item>
    ///   <item><description>The value is not null and is greater than the maximum allowed value</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Null values are considered valid and will not generate any validation errors.
    /// Both minimum and maximum values are inclusive, meaning a value equal to 
    /// either boundary is considered valid.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
    /// </exception>
    public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
      ArgumentNullException.ThrowIfNull(instance);
      ArgumentNullException.ThrowIfNull(context);

      var value = _PropertyAccessor(instance);

      // Null values are considered valid
      if (!value.HasValue) {
        yield break;
      }

      var actualValue = value.Value;

      if (actualValue.CompareTo(_Min) < 0) {
        yield return new ValidationError(
          "OUT_OF_RANGE_MIN",
          $"{_PropertyName} must be at least {_Min}.",
          _PropertyName,
          _Min
        );
      }

      if (actualValue.CompareTo(_Max) > 0) {
        yield return new ValidationError(
          "OUT_OF_RANGE_MAX",
          $"{_PropertyName} must not exceed {_Max}.",
          _PropertyName,
          _Max
        );
      }
    }

    /// <summary>
    /// Extracts the property name from an expression.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The name of the property accessed by the expression.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the expression does not represent a simple property access.
    /// </exception>
    private static string GetPropertyName(Expression<Func<T, TValue?>> expression) =>
      expression.Body is MemberExpression member
        ? member.Member.Name
        : throw new ArgumentException("Invalid property expression");
  }
}
