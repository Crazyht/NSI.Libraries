namespace NSI.Core.Validation.Abstractions;
/// <summary>
/// Represents a single validation error that occurs during object validation.
/// </summary>
/// <remarks>
/// <para>
/// Validation errors provide structured information about validation failures,
/// including which property failed validation (if applicable), error codes,
/// human-readable messages, and expected values.
/// </para>
/// <para>
/// These errors can be collected, aggregated, and presented to users or
/// transformed into API responses.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var errors = await validator.ValidateAsync(user);
/// foreach (var error in errors) {
///   Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage} [{error.ErrorCode}]");
/// }
/// </code>
/// </para>
/// </remarks>
public interface IValidationError {
  /// <summary>
  /// Gets the name of the property that failed validation.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Can be null for cross-field validation rules or entity-level validations
  /// that don't apply to a specific property.
  /// </para>
  /// <para>
  /// For nested properties, a dot notation may be used (e.g., "Address.City").
  /// </para>
  /// </remarks>
  public string? PropertyName { get; }

  /// <summary>
  /// Gets the error code in UPPERCASE format.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Error codes should be consistent across the application and follow
  /// the naming convention of uppercase letters with underscores.
  /// </para>
  /// <para>
  /// Common examples include: "REQUIRED", "INVALID_FORMAT", "MAX_LENGTH_EXCEEDED".
  /// </para>
  /// </remarks>
  public string ErrorCode { get; }

  /// <summary>
  /// Gets the human-readable error message in English.
  /// </summary>
  /// <remarks>
  /// The message should be clear, concise, and suitable for display to end users
  /// or developers. It should explain what went wrong and possibly suggest how to fix it.
  /// </remarks>
  public string ErrorMessage { get; }

  /// <summary>
  /// Gets the expected value, if applicable.
  /// </summary>
  /// <remarks>
  /// <para>
  /// For validation rules that check against specific values or ranges,
  /// this property can provide the expected value(s) that would pass validation.
  /// </para>
  /// <para>
  /// May be null if not applicable to the specific validation rule.
  /// </para>
  /// </remarks>
  public object? ExpectedValue { get; }
}
