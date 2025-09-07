using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that a value is unique by checking against a data source.
/// </summary>
/// <typeparam name="T">The type of object being validated.</typeparam>
/// <typeparam name="TValue">The type of the property value.</typeparam>
/// <remarks>
/// <para>
/// This validation rule checks that a property value is unique in the system,
/// typically by performing an asynchronous lookup against a database or other data store.
/// It's commonly used for enforcing uniqueness constraints on identifiers,
/// usernames, email addresses, or other business keys.
/// </para>
/// <para>
/// The rule requires a function that performs the actual uniqueness check. This function
/// should access the appropriate repository or service through the provided
/// <see cref="IServiceProvider"/>.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Validate that email is unique
/// var emailRule = new UniqueRule&lt;User, string&gt;(
///   u => u.Email,
///   async (serviceProvider, email, ct) => {
///     var userRepo = serviceProvider.GetRequiredService&lt;IUserRepository&gt;();
///     return await userRepo.EmailExistsAsync(email, ct);
///   }
/// );
/// 
/// var errors = await emailRule.ValidateAsync(user, validationContext);
/// </code>
/// </para>
/// </remarks>
public sealed class UniqueRule<T, TValue>: IAsyncValidationRule<T> {
  private readonly Func<T, TValue> _PropertyAccessor;
  private readonly string _PropertyName;
  private readonly Func<IServiceProvider, TValue, CancellationToken, Task<bool>> _ExistsCheck;

  /// <summary>
  /// Initializes a new instance of the <see cref="UniqueRule{T, TValue}"/> class.
  /// </summary>
  /// <param name="propertyExpression">Expression to access the property to check for uniqueness.</param>
  /// <param name="existsCheck">
  /// Function to check if the value already exists. Should return true if the value exists 
  /// (meaning validation fails) or false if the value is unique (validation passes).
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="propertyExpression"/> or <paramref name="existsCheck"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="propertyExpression"/> is not a valid member expression.
  /// </exception>
  /// <remarks>
  /// The property expression should be a simple member access expression
  /// (e.g., <c>x => x.PropertyName</c>) that returns the value to check for uniqueness.
  /// </remarks>
  public UniqueRule(
    Expression<Func<T, TValue>> propertyExpression,
    Func<IServiceProvider, TValue, CancellationToken, Task<bool>> existsCheck) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentNullException.ThrowIfNull(existsCheck);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression);
    _ExistsCheck = existsCheck;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// This method validates that the property value is unique by executing the
  /// exists check function provided in the constructor. If the value already exists,
  /// it returns a validation error.
  /// </para>
  /// <para>
  /// The method will:
  /// <list type="bullet">
  ///   <item><description>Return an empty collection if the property value is null</description></item>
  ///   <item><description>Return a validation error if the exists check returns true</description></item>
  ///   <item><description>Return an empty collection if the exists check returns false</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="context"/> or <paramref name="instance"/> is null.
  /// </exception>
  public async Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(instance);

    var value = _PropertyAccessor(instance);

    if (value is null) {
      return [];
    }

    var exists = await _ExistsCheck(context.ServiceProvider, value, cancellationToken);

    if (exists) {
      return [
        new ValidationError(
          "NOT_UNIQUE",
          $"{_PropertyName} already exists.",
          _PropertyName
        )
      ];
    }

    return [];
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
