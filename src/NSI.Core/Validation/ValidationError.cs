using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation {
  /// <summary>
  /// Default implementation of <see cref="IValidationError"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides a concrete implementation of the validation error interface
  /// that can be used to represent validation failures throughout the application.
  /// </para>
  /// <para>
  /// ValidationError objects contain structured information about what validation failed,
  /// including:
  /// <list type="bullet">
  ///   <item><description>The property that failed validation (if applicable)</description></item>
  ///   <item><description>A standardized error code that can be used for localization or categorization</description></item>
  ///   <item><description>A human-readable error message</description></item>
  ///   <item><description>The expected value that would have passed validation (optional)</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// var error = new ValidationError(
  ///   errorCode: "REQUIRED",
  ///   errorMessage: "Email address is required.",
  ///   propertyName: "Email"
  /// );
  /// </code>
  /// </para>
  /// </remarks>
  public readonly record struct ValidationError: IValidationError {
    /// <inheritdoc/>
    public string? PropertyName { get; }

    /// <inheritdoc/>
    public string ErrorCode { get; }

    /// <inheritdoc/>
    public string ErrorMessage { get; }

    /// <inheritdoc/>
    public object? ExpectedValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="errorCode">The error code in UPPERCASE format.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="expectedValue">The expected value, if applicable.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="errorCode"/> or <paramref name="errorMessage"/> is null or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Error codes should follow the UPPERCASE_WITH_UNDERSCORES convention
    /// and be consistent across the application. Common examples include:
    /// "REQUIRED", "INVALID_FORMAT", "NOT_UNIQUE", etc.
    /// </para>
    /// <para>
    /// The error message should be clear, concise, and suitable for end-user display.
    /// </para>
    /// </remarks>
    public ValidationError(
      string errorCode,
      string errorMessage,
      string? propertyName = null,
      object? expectedValue = null) {
      ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
      ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

      ErrorCode = errorCode;
      ErrorMessage = errorMessage;
      PropertyName = propertyName;
      ExpectedValue = expectedValue;
    }
  }
}
