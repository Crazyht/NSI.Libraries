using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;
/// <summary>
/// Validates each item in a collection property using its registered validator.
/// </summary>
/// <typeparam name="T">The type of the parent object.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// <para>
/// This validation rule recursively applies validations to each item in a collection
/// property by resolving the appropriate <see cref="IValidator{TItem}"/> from the
/// dependency injection container.
/// </para>
/// <para>
/// The rule will:
/// <list type="bullet">
///   <item><description>Locate the property using the provided expression</description></item>
///   <item><description>Skip validation if the collection is null</description></item>
///   <item><description>Resolve the appropriate validator for each item type</description></item>
///   <item><description>Apply validation to each non-null item in the collection</description></item>
///   <item><description>Prefix property names in validation errors with the collection path</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Validate all items in the user's Addresses collection
/// var addressesRule = new CollectionValidationRule&lt;User, Address&gt;(u => u.Addresses);
/// var errors = addressesRule.Validate(user, validationContext);
/// </code>
/// </para>
/// </remarks>
public sealed class CollectionValidationRule<T, TItem>: IValidationRule<T>
  where TItem : class {
  private readonly Func<T, IEnumerable<TItem>?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Initializes a new instance of the <see cref="CollectionValidationRule{T,TItem}"/> class.
  /// </summary>
  /// <param name="propertyExpression">Expression to access the collection property.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="propertyExpression"/> is not a valid member expression.
  /// </exception>
  /// <remarks>
  /// The property expression should be a simple member access expression
  /// (e.g., <c>x => x.Items</c>) that returns a collection of <typeparamref name="TItem"/>.
  /// </remarks>
  public CollectionValidationRule(Expression<Func<T, IEnumerable<TItem>?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// This method validates each item in the collection property specified in the constructor.
  /// It resolves the appropriate <see cref="IValidator{TItem}"/> from the dependency
  /// injection container and applies it to each non-null item.
  /// </para>
  /// <para>
  /// The method will:
  /// <list type="bullet">
  ///   <item><description>Return an empty collection if the property value is null</description></item>
  ///   <item><description>Return an empty collection if no validator is registered for <typeparamref name="TItem"/></description></item>
  ///   <item><description>Skip null items in the collection</description></item>
  ///   <item><description>Prefix all property names in errors with the collection property name and index</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="context"/> is null.
  /// </exception>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(instance);

    var collection = _PropertyAccessor(instance);

    if (collection is null) {
      yield break;
    }

    var validator = context.ServiceProvider?.GetService<IValidator<TItem>>();

    if (validator is null) {
      yield break;
    }

    var index = 0;
    foreach (var item in collection) {
      if (item is null) {
        index++;
        continue;
      }

      var result = validator.Validate(item, context);

      foreach (var error in result.Errors) {
        var prefixedPropertyName = string.IsNullOrEmpty(error.PropertyName)
          ? $"{_PropertyName}[{index}]"
          : $"{_PropertyName}[{index}].{error.PropertyName}";

        yield return new ValidationError(
          error.ErrorCode,
          error.ErrorMessage,
          prefixedPropertyName,
          error.ExpectedValue
        );
      }

      index++;
    }
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression member => member.Member.Name,
    LambdaExpression lambda => GetPropertyName(lambda.Body),
    _ => throw new ArgumentException("Invalid property expression")
  };
}
