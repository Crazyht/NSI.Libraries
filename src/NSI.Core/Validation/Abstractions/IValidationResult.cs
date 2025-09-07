namespace NSI.Core.Validation.Abstractions;
/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a standardized way to represent validation results
/// throughout the application. It includes both a boolean indicator of validity
/// and a collection of any validation errors that occurred.
/// </para>
/// <para>
/// Validation results can be used to:
/// <list type="bullet">
///   <item><description>Determine if an object is valid before processing</description></item>
///   <item><description>Collect and return detailed validation errors to clients</description></item>
///   <item><description>Aggregate validation results from multiple validation operations</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var result = await validator.ValidateAsync(user);
/// if (!result.IsValid) {
///   foreach (var error in result.Errors) {
///     Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IValidationResult {
  /// <summary>
  /// Gets a value indicating whether the validation passed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Returns <see langword="true"/> when no validation errors were found,
  /// otherwise <see langword="false"/>.
  /// </para>
  /// <para>
  /// This property provides a quick way to check validation status without
  /// examining the individual errors.
  /// </para>
  /// </remarks>
  public bool IsValid { get; }

  /// <summary>
  /// Gets the collection of validation errors.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Returns an empty collection when validation passes (IsValid is true).
  /// </para>
  /// <para>
  /// Each <see cref="IValidationError"/> in the collection contains detailed
  /// information about a specific validation failure, including the property name,
  /// error code, and a human-readable error message.
  /// </para>
  /// <para>
  /// The collection is read-only to prevent modification after validation is complete.
  /// </para>
  /// </remarks>
  public IReadOnlyList<IValidationError> Errors { get; }
}
