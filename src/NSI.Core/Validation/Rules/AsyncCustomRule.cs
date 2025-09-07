using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;
/// <summary>
/// Allows creating custom asynchronous validation rules with lambda expressions.
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
/// var emailRule = new AsyncCustomRule&lt;User&gt;(async (user, context, ct) => {
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
public sealed class AsyncCustomRule<T>: IAsyncValidationRule<T> {
  private readonly Func<T, IValidationContext, CancellationToken, Task<IEnumerable<IValidationError>>> _ValidateFunc;

  /// <summary>
  /// Initializes a new instance of the <see cref="AsyncCustomRule{T}"/> class.
  /// </summary>
  /// <param name="validateFunc">The async validation function that implements the rule logic.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validateFunc"/> is null.
  /// </exception>
  /// <remarks>
  /// The validation function must follow the same contract as the 
  /// <see cref="IAsyncValidationRule{T}.ValidateAsync"/> method, accepting the object
  /// to validate, a validation context, and a cancellation token.
  /// </remarks>
  public AsyncCustomRule(
    Func<T, IValidationContext, CancellationToken, Task<IEnumerable<IValidationError>>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validateFunc);
    _ValidateFunc = validateFunc;
  }

  /// <inheritdoc/>
  public Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default) => _ValidateFunc(instance, context, cancellationToken);
}
