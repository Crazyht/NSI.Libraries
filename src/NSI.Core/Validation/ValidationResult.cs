using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation {
  /// <summary>
  /// Default implementation of <see cref="IValidationResult"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides a concrete implementation of the validation result
  /// interface, with convenience methods for creating successful and failed 
  /// validation results.
  /// </para>
  /// <para>
  /// Key features:
  /// <list type="bullet">
  ///   <item><description>Immutable result object with read-only error collection</description></item>
  ///   <item><description>Static factory methods for common validation scenarios</description></item>
  ///   <item><description>Automatic IsValid calculation based on error presence</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// // Create a successful result
  /// var success = ValidationResult.Success;
  /// 
  /// // Create a failed result with custom error
  /// var failed = ValidationResult.Failed(
  ///   "INVALID_EMAIL",
  ///   "Email format is invalid.",
  ///   "Email"
  /// );
  /// 
  /// // Check if validation passed
  /// if (!result.IsValid) {
  ///   foreach (var error in result.Errors) {
  ///     Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
  ///   }
  /// }
  /// </code>
  /// </para>
  /// </remarks>
  public sealed class ValidationResult: IValidationResult {
    /// <inheritdoc/>
    public bool IsValid => Errors.Count == 0;

    /// <inheritdoc/>
    public IReadOnlyList<IValidationError> Errors { get; }

    /// <summary>
    /// Gets a successful validation result with no errors.
    /// </summary>
    /// <remarks>
    /// This shared instance can be used whenever a successful validation result is needed,
    /// avoiding unnecessary object creation.
    /// </remarks>
    public static ValidationResult Success { get; } = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="errors">The collection of validation errors.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> is null.
    /// </exception>
    /// <remarks>
    /// The errors collection is converted to a read-only list to ensure immutability
    /// of the validation result after creation.
    /// </remarks>
    public ValidationResult(IEnumerable<IValidationError> errors) {
      ArgumentNullException.ThrowIfNull(errors);
      Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors to include in the result.</param>
    /// <returns>A new validation result containing the errors.</returns>
    /// <remarks>
    /// This method allows passing multiple validation errors using params syntax.
    /// </remarks>
    public static ValidationResult Failed(params IValidationError[] errors) => new(errors);

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="errorCode">The error code in UPPERCASE format.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <param name="propertyName">The name of the property that failed validation, or null for entity-level errors.</param>
    /// <param name="expectedValue">The expected value that would pass validation, if applicable.</param>
    /// <returns>A new validation result containing the error.</returns>
    /// <remarks>
    /// This convenience method creates a validation result with a single error, which is
    /// a common scenario for simple validation failures.
    /// </remarks>
    public static ValidationResult Failed(
      string errorCode,
      string errorMessage,
      string? propertyName = null,
      object? expectedValue = null) => new(
        [new ValidationError(errorCode, errorMessage, propertyName, expectedValue)]
      );
  }
}
