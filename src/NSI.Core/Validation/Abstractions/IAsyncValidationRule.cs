namespace NSI.Core.Validation.Abstractions;
/// <summary>
/// Defines an asynchronous validation rule that can be applied to an object.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
/// <remarks>
/// <para>
/// This interface should be implemented by classes that perform asynchronous validation
/// on domain objects. Each implementation represents a single validation rule.
/// </para>
/// <para>
/// Multiple validation rules can be combined and executed in sequence to validate
/// complex business requirements.
/// </para>
/// </remarks>
public interface IAsyncValidationRule<T> {
  /// <summary>
  /// Validates the specified object asynchronously.
  /// </summary>
  /// <param name="instance">The object to validate.</param>
  /// <param name="context">The validation context containing additional information.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation if needed.</param>
  /// <returns>
  /// A task that represents the asynchronous validation operation, resolving to an
  /// enumerable collection of validation errors. Returns an empty collection if validation passes.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
  /// </exception>
  /// <exception cref="OperationCanceledException">
  /// Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.
  /// </exception>
  public Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default);
}
