using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules {
  /// <summary>
  /// Validates that a numeric property value is within a specified range.
  /// </summary>
  /// <typeparam name="T">The type of object being validated.</typeparam>
  /// <typeparam name="TValue">The numeric type of the property.</typeparam>
  /// <remarks>
  /// <para>
  /// This validation rule checks that a property value falls within the inclusive
  /// range between the specified minimum and maximum values.
  /// </para>
  /// <para>
  /// The rule works with any type that implements <see cref="IComparable{T}"/>,
  /// making it suitable for various numeric types (int, decimal, double) as well as
  /// other comparable types like DateTime.
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// // Validate that Age is between 18 and 65
  /// var ageRule = new RangeRule&lt;User, int&gt;(u => u.Age, 18, 65);
  /// var errors = ageRule.Validate(user, validationContext);
  /// 
  /// // Validate price is between allowed bounds
  /// var priceRule = new RangeRule&lt;Product, decimal&gt;(p => p.Price, 0.01m, 9999.99m);
  /// </code>
  /// </para>
  /// </remarks>
  public sealed class RangeRule<T, TValue>: IValidationRule<T>
    where TValue : IComparable<TValue> {
    private readonly Func<T, TValue> _PropertyAccessor;
    private readonly string _PropertyName;
    private readonly TValue _Min;
    private readonly TValue _Max;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="propertyExpression">Expression to access the property.</param>
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
    /// (e.g., <c>x => x.PropertyName</c>) that returns a comparable value.
    /// </remarks>
    public RangeRule(
      Expression<Func<T, TValue>> propertyExpression,
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
    /// This method validates that the property value is within the range specified
    /// during initialization. It returns validation errors when the value is:
    /// <list type="bullet">
    ///   <item><description>Less than the minimum allowed value</description></item>
    ///   <item><description>Greater than the maximum allowed value</description></item>
    /// </list>
    /// </para>
    /// <para>
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

      if (value.CompareTo(_Min) < 0) {
        yield return new ValidationError(
          "OUT_OF_RANGE_MIN",
          $"{_PropertyName} must be at least {_Min}.",
          _PropertyName,
          _Min
        );
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

    /// <summary>
    /// Extracts the property name from an expression.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>The name of the property accessed by the expression.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the expression does not represent a simple property access.
    /// </exception>
    private static string GetPropertyName(Expression<Func<T, TValue>> expression) =>
      expression.Body is MemberExpression member
        ? member.Member.Name
        : throw new ArgumentException("Invalid property expression");
  }
}
