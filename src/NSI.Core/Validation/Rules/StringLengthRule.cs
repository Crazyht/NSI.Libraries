using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that a string property length is within specified bounds.
/// </summary>
/// <typeparam name="T">The type of object being validated.</typeparam>
/// <remarks>
/// <para>
/// This validation rule checks that a string property's length falls within
/// the specified minimum and maximum character limits.
/// </para>
/// <para>
/// Key behaviors:
/// <list type="bullet">
///   <item><description>Null strings are ignored (consider using <see cref="RequiredRule{T}"/> for null validation)</description></item>
///   <item><description>Empty strings pass validation if minLength is 0</description></item>
///   <item><description>Both minimum and maximum bounds are inclusive</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Validate that Username is between 3 and 50 characters
/// var usernameRule = new StringLengthRule&lt;User&gt;(u => u.Username, 3, 50);
/// var errors = usernameRule.Validate(user, validationContext);
/// 
/// // Validate that Description doesn't exceed 1000 characters
/// var descriptionRule = new StringLengthRule&lt;Product&gt;(p => p.Description, 0, 1000);
/// </code>
/// </para>
/// </remarks>
public sealed class StringLengthRule<T>: IValidationRule<T> {
  private readonly Func<T, string?> _PropertyAccessor;
  private readonly string _PropertyName;
  private readonly int _MinLength;
  private readonly int _MaxLength;

  /// <summary>
  /// Initializes a new instance of the <see cref="StringLengthRule{T}"/> class.
  /// </summary>
  /// <param name="propertyExpression">Expression to access the string property.</param>
  /// <param name="minLength">Minimum allowed length (inclusive, defaults to 0).</param>
  /// <param name="maxLength">Maximum allowed length (inclusive, defaults to int.MaxValue).</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="minLength"/> is negative or when 
  /// <paramref name="maxLength"/> is less than <paramref name="minLength"/>.
  /// </exception>
  /// <remarks>
  /// The property expression should be a simple member access expression
  /// (e.g., <c>x => x.PropertyName</c>) that returns a string value.
  /// </remarks>
  public StringLengthRule(
    Expression<Func<T, string?>> propertyExpression,
    int minLength = 0,
    int maxLength = int.MaxValue) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentOutOfRangeException.ThrowIfNegative(minLength);
    ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression);
    _MinLength = minLength;
    _MaxLength = maxLength;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// This method validates that the string property's length is within the specified bounds.
  /// It returns validation errors when:
  /// <list type="bullet">
  ///   <item><description>The string's length is less than the minimum allowed length</description></item>
  ///   <item><description>The string's length exceeds the maximum allowed length</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Null strings are ignored by this validation. To validate that a string is not null,
  /// use the <see cref="RequiredRule{T}"/> in conjunction with this rule.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
  /// </exception>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    if (value is null) {
      yield break;
    }

    if (value.Length < _MinLength) {
      yield return new ValidationError(
        "TOO_SHORT",
        $"{_PropertyName} must be at least {_MinLength} characters long.",
        _PropertyName,
        _MinLength
      );
    }

    if (value.Length > _MaxLength) {
      yield return new ValidationError(
        "TOO_LONG",
        $"{_PropertyName} must not exceed {_MaxLength} characters.",
        _PropertyName,
        _MaxLength
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
  private static string GetPropertyName(Expression<Func<T, string?>> expression) =>
    expression.Body is MemberExpression member
      ? member.Member.Name
      : throw new ArgumentException("Invalid property expression");
}
