using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation {
  /// <summary>
  /// Exception thrown when validation fails.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This exception is used to convert validation failures into exceptions that can
  /// interrupt the application flow. It wraps a <see cref="IValidationResult"/> containing
  /// all validation errors that occurred.
  /// </para>
  /// <para>
  /// Typical usage patterns:
  /// <list type="bullet">
  ///   <item><description>Throwing when validation fails in a domain service</description></item>
  ///   <item><description>Converting validation failures to HTTP 400 responses in API controllers</description></item>
  ///   <item><description>Interrupting business operations that require valid input</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// var result = validator.Validate(user);
  /// if (!result.IsValid) {
  ///   throw new ValidationException(result);
  /// }
  /// </code>
  /// </para>
  /// </remarks>
  public sealed class ValidationException: Exception {
    /// <summary>
    /// Gets the validation result containing all errors.
    /// </summary>
    /// <remarks>
    /// This property provides access to the structured validation errors
    /// that can be used for detailed error reporting.
    /// </remarks>
    public IValidationResult ValidationResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="validationResult">The validation result containing errors.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="validationResult"/> is null.
    /// </exception>
    /// <remarks>
    /// This constructor automatically generates an appropriate error message based on
    /// the validation errors contained in the validation result.
    /// </remarks>
    public ValidationException(IValidationResult validationResult)
      : this(GetMessage(validationResult), validationResult) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="validationResult">The validation result containing errors.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="validationResult"/> is null.
    /// </exception>
    /// <remarks>
    /// Use this constructor when you want to provide a custom error message
    /// rather than the auto-generated one.
    /// </remarks>
    public ValidationException(string message, IValidationResult validationResult)
      : base(message) {
      ArgumentNullException.ThrowIfNull(validationResult);
      ValidationResult = validationResult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <remarks>
    /// <para>
    /// This constructor is provided for compatibility with the standard Exception pattern,
    /// but should rarely be used since validation exceptions typically wrap validation
    /// results rather than other exceptions.
    /// </para>
    /// <para>
    /// When using this constructor, <see cref="ValidationResult"/> will be null,
    /// which may cause issues with code that expects it to be non-null.
    /// </para>
    /// </remarks>
    public ValidationException(string message, Exception innerException)
      : base(message, innerException) => ValidationResult = new EmptyValidationResult();

    /// <summary>
    /// Generates an appropriate error message based on the validation result.
    /// </summary>
    /// <param name="validationResult">The validation result containing errors.</param>
    /// <returns>A human-readable error message.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="validationResult"/> is null.
    /// </exception>
    private static string GetMessage(IValidationResult validationResult) {
      ArgumentNullException.ThrowIfNull(validationResult);

      var errorCount = validationResult.Errors.Count;
      if (errorCount == 1) {
        return $"Validation failed: {validationResult.Errors[0].ErrorMessage}";
      }

      return $"Validation failed with {errorCount} errors.";
    }

    /// <summary>
    /// Provides an empty validation result for use when no result is available.
    /// </summary>
    private sealed class EmptyValidationResult: IValidationResult {
      /// <inheritdoc/>
      public bool IsValid => true;

      /// <inheritdoc/>
      public IReadOnlyList<IValidationError> Errors => [];
    }
  }
}
