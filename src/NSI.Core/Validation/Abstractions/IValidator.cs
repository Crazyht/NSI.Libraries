namespace NSI.Core.Validation.Abstractions;
/// <summary>
/// Defines a validator that can validate objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
/// <remarks>
/// <para>
/// This interface defines the core validation contract for domain objects.
/// Implementations typically combine multiple validation rules into a cohesive
/// validation strategy for a specific domain type.
/// </para>
/// <para>
/// Validators provide both synchronous and asynchronous validation methods to
/// accommodate different usage scenarios. The asynchronous method is preferred
/// when validation involves I/O operations or external service calls.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var validator = new UserValidator();
/// var result = await validator.ValidateAsync(user);
/// 
/// if (!result.IsValid) {
///   foreach (var error in result.Errors) {
///     Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IValidator<T> {
  /// <summary>
  /// Validates the specified object synchronously.
  /// </summary>
  /// <param name="instance">The object to validate.</param>
  /// <param name="context">Optional validation context providing additional validation data and services.</param>
  /// <returns>
  /// The validation result containing validation status and any validation errors.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Use this method when all validation can be performed synchronously without
  /// I/O operations or when immediate validation results are required.
  /// </para>
  /// <para>
  /// If <paramref name="context"/> is not provided, a default context may be created
  /// by the implementation.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> is null.
  /// </exception>
  public IValidationResult Validate(T instance, IValidationContext? context = null);

  /// <summary>
  /// Validates the specified object asynchronously.
  /// </summary>
  /// <param name="instance">The object to validate.</param>
  /// <param name="context">Optional validation context providing additional validation data and services.</param>
  /// <param name="cancellationToken">
  /// A cancellation token that can be used to cancel the validation operation.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous validation operation, resolving to a
  /// validation result containing validation status and any validation errors.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method is preferred when validation requires asynchronous operations such as
  /// database queries, external API calls, or other I/O-bound operations.
  /// </para>
  /// <para>
  /// If <paramref name="context"/> is not provided, a default context may be created
  /// by the implementation.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> is null.
  /// </exception>
  /// <exception cref="OperationCanceledException">
  /// Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.
  /// </exception>
  public Task<IValidationResult> ValidateAsync(
    T instance,
    IValidationContext? context = null,
    CancellationToken cancellationToken = default);
}
