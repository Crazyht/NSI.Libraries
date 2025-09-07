using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;
/// <summary>
/// Validates a nested object property using its registered validator.
/// </summary>
/// <typeparam name="T">The type of the parent object.</typeparam>
/// <typeparam name="TProperty">The type of the nested property.</typeparam>
/// <remarks>
/// <para>
/// This validation rule performs recursive validation on a nested object property
/// by resolving the appropriate <see cref="IValidator{TProperty}"/> from the 
/// dependency injection container.
/// </para>
/// <para>
/// The rule will:
/// <list type="bullet">
///   <item><description>Access the property using the provided expression</description></item>
///   <item><description>Skip validation if the property value is null</description></item>
///   <item><description>Resolve the appropriate validator for the property type</description></item>
///   <item><description>Apply validation to the nested object</description></item>
///   <item><description>Prefix property names in validation errors with the parent property path</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Validate a user's Address property
/// var addressRule = new NestedValidationRule&lt;User, Address&gt;(u => u.Address);
/// var errors = addressRule.Validate(user, validationContext);
/// </code>
/// </para>
/// </remarks>
public sealed class NestedValidationRule<T, TProperty>: IValidationRule<T>
  where TProperty : class {
  private readonly Func<T, TProperty?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Initializes a new instance of the <see cref="NestedValidationRule{T,TProperty}"/> class.
  /// </summary>
  /// <param name="propertyExpression">Expression to access the nested property.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="propertyExpression"/> is not a valid member expression.
  /// </exception>
  /// <remarks>
  /// The property expression should be a simple member access expression
  /// (e.g., <c>x => x.Property</c>) that returns a reference type object.
  /// </remarks>
  public NestedValidationRule(Expression<Func<T, TProperty?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// This method validates the nested object property specified in the constructor.
  /// It resolves the appropriate <see cref="IValidator{TProperty}"/> from the dependency
  /// injection container and applies it to the property value.
  /// </para>
  /// <para>
  /// The method will:
  /// <list type="bullet">
  ///   <item><description>Return an empty collection if the property value is null</description></item>
  ///   <item><description>Return an empty collection if no validator is registered for <typeparamref name="TProperty"/></description></item>
  ///   <item><description>Prefix all property names in errors with the parent property name</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="context"/> or <paramref name="instance"/> is null.
  /// </exception>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(instance);

    var propertyValue = _PropertyAccessor(instance);

    if (propertyValue is null) {
      yield break;
    }

    // Try to get validator from service provider
    var validator = context.ServiceProvider?.GetService<IValidator<TProperty>>();

    if (validator is null) {
      yield break;
    }

    var result = validator.Validate(propertyValue, context);

    // Prefix property names with parent property name
    foreach (var error in result.Errors) {
      var prefixedPropertyName = string.IsNullOrEmpty(error.PropertyName)
        ? _PropertyName
        : $"{_PropertyName}.{error.PropertyName}";

      yield return new ValidationError(
        error.ErrorCode,
        error.ErrorMessage,
        prefixedPropertyName,
        error.ExpectedValue
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
  private static string GetPropertyName(Expression<Func<T, TProperty?>> expression) =>
      expression.Body is MemberExpression member
      ? member.Member.Name
      : throw new ArgumentException("Invalid property expression");
}
