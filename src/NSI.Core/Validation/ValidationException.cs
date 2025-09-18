using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation;

/// <summary>
/// Exception representing a failed validation pipeline (encapsulates structured errors).
/// </summary>
/// <remarks>
/// <para>
/// Converts a <see cref="IValidationResult"/> into a failure signal for flows that prefer exceptions
/// (e.g. API controller filters, domain service guards, integration boundaries). The original
/// validation metadata remains accessible through <see cref="ValidationResult"/> for problem details
/// projection, logging or localization.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Never thrown for system faults (only business / input validation).</description></item>
///   <item><description><see cref="ValidationResult"/> is never null (empty result when constructed with an inner exception).</description></item>
///   <item><description>Error message auto-generated from first error (singular) or error count.</description></item>
///   <item><description>Does not mutate or filter the underlying errors.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer returning <see cref="IValidationResult"/> in pure domain layers; throw only at boundaries.</description></item>
///   <item><description>Do not catch and swallow this exception; translate to user-facing response.</description></item>
///   <item><description>Preserve original error ordering for deterministic client display.</description></item>
///   <item><description>Avoid wrapping this exception again; enrich via logging context instead.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable after construction. Safe to share between threads.</para>
/// <para>Performance: Lightweight; message generation is O(1). Errors collection not copied.</para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateAsync(user, context, ct);
/// if (!result.IsValid) {
///   throw new ValidationException(result);
/// }
/// </code>
/// </example>
public sealed class ValidationException: Exception {
  /// <summary>
  /// Gets the structured validation outcome (never null).
  /// </summary>
  public IValidationResult ValidationResult { get; }

  /// <summary>
  /// Creates an instance using an auto-generated message derived from the validation errors.
  /// </summary>
  /// <param name="validationResult">Result containing at least one error.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationResult"/> is null.</exception>
  public ValidationException(IValidationResult validationResult)
    : this(GetMessage(validationResult), validationResult) { }

  /// <summary>
  /// Creates an instance with a custom message and associated validation result.
  /// </summary>
  /// <param name="message">Custom error message.</param>
  /// <param name="validationResult">Result containing validation errors.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationResult"/> is null.</exception>
  public ValidationException(string message, IValidationResult validationResult) : base(message) {
    ArgumentNullException.ThrowIfNull(validationResult);
    ValidationResult = validationResult;
  }

  /// <summary>
  /// Creates an instance following standard exception chaining (empty validation result).
  /// </summary>
  /// <param name="message">Error message.</param>
  /// <param name="innerException">Underlying exception.</param>
  public ValidationException(string message, Exception innerException) : base(message, innerException) =>
    ValidationResult = EmptyValidationResult.Instance;

  private static string GetMessage(IValidationResult validationResult) {
    ArgumentNullException.ThrowIfNull(validationResult);
    var count = validationResult.Errors.Count;
    return count switch {
      0 => "Validation failed.",
      1 => $"Validation failed: {validationResult.Errors[0].ErrorMessage}",
      _ => $"Validation failed with {count} errors."
    };
  }

  /// <summary>
  /// Singleton empty validation result used when no explicit errors are supplied (chaining ctor).
  /// </summary>
  private sealed class EmptyValidationResult: IValidationResult {
    internal static readonly EmptyValidationResult Instance = new();
    public bool IsValid => true;
    public IReadOnlyList<IValidationError> Errors => [];
  }
}
