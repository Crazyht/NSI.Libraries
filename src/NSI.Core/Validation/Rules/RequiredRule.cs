using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;
/// <summary>
/// Validates that a property value is not null, empty, or whitespace.
/// </summary>
/// <typeparam name="T">The type of object being validated.</typeparam>
/// <remarks>
/// <para>
/// This validation rule checks that a property has a valid, non-empty value.
/// It handles different types of properties:
/// <list type="bullet">
///   <item><description>For string properties, checks that the value is not null, empty, or whitespace</description></item>
///   <item><description>For reference type properties, checks that the value is not null</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var nameRule = new RequiredRule&lt;User&gt;(u => u.Name);
/// var errors = nameRule.Validate(user, validationContext);
/// </code>
/// </para>
/// </remarks>
public sealed class RequiredRule<T>: IValidationRule<T> {
  private readonly Func<T, object?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Initializes a new instance of the <see cref="RequiredRule{T}"/> class.
  /// </summary>
  /// <param name="propertyExpression">Expression to access the property.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="propertyExpression"/> is not a valid member expression.
  /// </exception>
  /// <remarks>
  /// The property expression should be a simple member access expression
  /// (e.g., <c>x => x.PropertyName</c>) that returns any object type.
  /// </remarks>
  public RequiredRule(Expression<Func<T, object?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// This method validates that the property value is not null, empty, or whitespace.
  /// For string properties, it checks for null, empty, or whitespace values.
  /// For non-string properties, it checks for null values only.
  /// </para>
  /// <para>
  /// A validation error is returned when:
  /// <list type="bullet">
  ///   <item><description>The property value is null</description></item>
  ///   <item><description>The property value is a string that is empty or contains only whitespace</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
  /// </exception>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    if (value is null || (value is string str && string.IsNullOrWhiteSpace(str))) {
      yield return new ValidationError(
        "REQUIRED",
        $"{_PropertyName} is required.",
        _PropertyName,
        "non-empty value"
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
  private static string GetPropertyName(Expression<Func<T, object?>> expression) => expression.Body switch {
    MemberExpression member => member.Member.Name,
    UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
    _ => throw new ArgumentException("Invalid property expression")
  };
}
