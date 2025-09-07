using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules {
  /// <summary>
  /// Allows creating custom validation rules with lambda expressions.
  /// </summary>
  /// <typeparam name="T">The type of object being validated.</typeparam>
  /// <remarks>
  /// <para>
  /// This class provides a flexible way to create validation rules on-the-fly using lambda
  /// expressions, without needing to create separate concrete rule classes for simple validations.
  /// </para>
  /// <para>
  /// It's particularly useful for:
  /// <list type="bullet">
  ///   <item><description>One-off validation rules</description></item>
  ///   <item><description>Dynamically constructed validation pipelines</description></item>
  ///   <item><description>Testing scenarios</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// var emailRule = new CustomRule&lt;User&gt;((user, context) => {
  ///   if (string.IsNullOrEmpty(user.Email) || !IsValidEmail(user.Email)) {
  ///     return new[] { new ValidationError {
  ///       PropertyName = nameof(User.Email),
  ///       ErrorCode = "INVALID_FORMAT",
  ///       ErrorMessage = "Email format is invalid."
  ///     }};
  ///   }
  ///   return Enumerable.Empty&lt;IValidationError&gt;();
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public sealed class CustomRule<T>: IValidationRule<T> {
    private readonly Func<T, IValidationContext, IEnumerable<IValidationError>> _ValidateFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRule{T}"/> class.
    /// </summary>
    /// <param name="validateFunc">The validation function that implements the rule logic.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="validateFunc"/> is null.
    /// </exception>
    /// <remarks>
    /// The validation function must follow the same contract as the 
    /// <see cref="IValidationRule{T}.Validate"/> method, accepting the object
    /// to validate and a validation context.
    /// </remarks>
    public CustomRule(Func<T, IValidationContext, IEnumerable<IValidationError>> validateFunc) {
      ArgumentNullException.ThrowIfNull(validateFunc);
      _ValidateFunc = validateFunc;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This implementation delegates to the validation function provided in the constructor.
    /// </remarks>
    public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) =>
      _ValidateFunc(instance, context);
  }
}
