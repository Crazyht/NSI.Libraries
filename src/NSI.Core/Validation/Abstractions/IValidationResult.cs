namespace NSI.Core.Validation.Abstractions;

/// <summary>
/// Aggregates the outcome of validating an object (success flag + immutable error list).
/// </summary>
/// <remarks>
/// <para>
/// Provides a standardized contract so validation pipelines, mediators and API layers can
/// interrogate validation outcomes uniformly without coupling to concrete implementations.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description><see cref="IsValid"/> is true when there are no errors.</description></item>
///   <item><description><see cref="Errors"/> is an immutable sequence (never <see langword="null"/>).</description></item>
///   <item><description>Error ordering should reflect rule evaluation order (stable for determinism).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Return a cached empty instance for the success case where practical.</description></item>
///   <item><description>Do not expose mutable collections; defensive copy if needed.</description></item>
///   <item><description>Never throw to signal business validation failure—use errors.</description></item>
///   <item><description>Keep error codes stable for client compatibility.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Implementations should be immutable; consumers treat instances as read-only.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await validator.ValidateAsync(user, context, ct);
/// if (!result.IsValid) {
///   foreach (var err in result.Errors) {
///     Console.WriteLine($"{err.PropertyName}: {err.ErrorMessage} ({err.ErrorCode})");
///   }
/// }
/// </code>
/// </example>
public interface IValidationResult {
  /// <summary>
  /// Gets a value indicating whether validation produced zero errors.
  /// </summary>
  /// <remarks>Equivalent to <c>Errors.Count == 0</c>. Always deterministic.</remarks>
  public bool IsValid { get; }

  /// <summary>
  /// Gets the immutable collection of validation errors (empty when <see cref="IsValid"/> is true).
  /// </summary>
  /// <remarks>
  /// Each <see cref="IValidationError"/> contains structured failure metadata (property, code, message).
  /// The list reference and its elements must not be mutated after creation.
  /// </remarks>
  public IReadOnlyList<IValidationError> Errors { get; }
}
