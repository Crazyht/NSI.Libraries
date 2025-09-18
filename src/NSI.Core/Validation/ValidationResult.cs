using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation;

/// <summary>
/// Immutable aggregation of validation errors (success when collection is empty).
/// </summary>
/// <remarks>
/// <para>
/// Encapsulates the outcome of validating an object. Provides a canonical structure consumed by
/// domain services, pipelines, mediators and presentation layers to drive control flow or error
/// projection (e.g. ProblemDetails / API responses).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="IsValid"/> is true iff <see cref="Errors"/> count equals zero.</description></item>
///   <item><description><see cref="Errors"/> is never null; ordering preserves rule evaluation order.</description></item>
///   <item><description>Instance is immutable after construction (defensive copy performed).</description></item>
///   <item><description>Shared singleton used for success path (<see cref="Success"/>).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Return <see cref="Success"/> instead of allocating new empty instances.</description></item>
///   <item><description>Prefer aggregating multiple rule errors rather than throwing exceptions.</description></item>
///   <item><description>Keep error ordering deterministic for UI predictability and test stability.</description></item>
///   <item><description>Do not mutate error objects; treat them as value semantics.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable; safe for concurrent read access.</para>
/// <para>Performance: Success path reuses a cached instance; failure path performs a single list copy.
/// Additional allocations scale linearly with number of errors only.</para>
/// </remarks>
/// <example>
/// <code>
/// var result = validator.Validate(user, context);
/// if (!result.IsValid) {
///   foreach (var e in result.Errors) {
///     Console.WriteLine($"{e.PropertyName}: {e.ErrorMessage}");
///   }
/// }
/// </code>
/// </example>
public sealed class ValidationResult: IValidationResult {
  /// <inheritdoc />
  public bool IsValid => Errors.Count == 0;

  /// <inheritdoc />
  public IReadOnlyList<IValidationError> Errors { get; }

  /// <summary>
  /// Shared empty successful instance (no allocations on success path).
  /// </summary>
  public static ValidationResult Success { get; } = new([], skipCopy: true);

  /// <summary>
  /// Creates a result from an existing error sequence (defensive copy unless flagged).
  /// </summary>
  /// <param name="errors">Source error sequence (enumerated immediately).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
  public ValidationResult(IEnumerable<IValidationError> errors) : this(errors, skipCopy: false) { }

  private ValidationResult(IEnumerable<IValidationError> errors, bool skipCopy) {
    ArgumentNullException.ThrowIfNull(errors);
    Errors = skipCopy && errors is IReadOnlyList<IValidationError> r
      ? r
      : errors.ToList().AsReadOnly();
  }

  /// <summary>
  /// Creates a failed result holding provided errors (params convenience).
  /// </summary>
  public static ValidationResult Failed(params IValidationError[] errors) =>
    errors is { Length: 0 } ? Success : new ValidationResult(errors, skipCopy: false);

  /// <summary>
  /// Creates a failed result with a single error (common fast path).
  /// </summary>
  public static ValidationResult Failed(
    string errorCode,
    string errorMessage,
    string? propertyName = null,
    object? expectedValue = null) => new([
      new ValidationError(errorCode, errorMessage, propertyName, expectedValue)
    ], skipCopy: true);
}
