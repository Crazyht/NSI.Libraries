namespace NSI.Core.Validation.Abstractions;

/// <summary>
/// Represents a single validation failure discovered while validating a domain object.
/// </summary>
/// <remarks>
/// <para>
/// A validation error conveys structured details (property, code, message, expected value) so
/// consumers can display precise feedback, localize messages, or map to problem details /
/// API error payloads.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description><see cref="PropertyName"/> identifies the failing member; may be null for object-level rules.</description></item>
///   <item><description><see cref="ErrorCode"/> is a stable UPPER_SNAKE_CASE token (machine key).</description></item>
///   <item><description><see cref="ErrorMessage"/> is end‑user / developer oriented, English baseline text.</description></item>
///   <item><description><see cref="ExpectedValue"/> supplies comparator data (length limit, pattern, etc.).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Do not embed PII in <see cref="ErrorMessage"/>.</description></item>
///   <item><description>Keep <see cref="ErrorCode"/> constant once published (backwards compatibility).</description></item>
///   <item><description>Prefer concise, actionable messages (&lt;= 120 chars).</description></item>
///   <item><description>Use dot notation for nested members (e.g. <c>Address.City</c>).</description></item>
///   <item><description>Leave <see cref="ExpectedValue"/> null when not meaningful.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Implementations should be immutable; consumers must treat instances as read-only.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enumeration example
/// var errors = await validator.ValidateAsync(order, context, ct);
/// foreach (var e in errors) {
///   Console.WriteLine($"{e.PropertyName}: {e.ErrorMessage} ({e.ErrorCode})");
/// }
/// </code>
/// </example>
public interface IValidationError {
  /// <summary>
  /// Gets the failing property/member logical path or null for an aggregate / object-level failure.
  /// </summary>
  /// <remarks>Nested members use dot notation (e.g. <c>User.Address.ZipCode</c>).</remarks>
  public string? PropertyName { get; }

  /// <summary>
  /// Gets the stable UPPER_SNAKE_CASE error code (machine readable classifier).
  /// </summary>
  /// <remarks>Must remain stable for clients relying on programmatic handling.</remarks>
  public string ErrorCode { get; }

  /// <summary>
  /// Gets the human-readable validation failure message (English baseline text).
  /// </summary>
  /// <remarks>Should not contain sensitive data; suitable for direct display or localization key mapping.</remarks>
  public string ErrorMessage { get; }

  /// <summary>
  /// Gets an optional expected/comparator value associated with the rule (e.g. max length, pattern).
  /// </summary>
  /// <remarks>Null when no comparator or reference value applies.</remarks>
  public object? ExpectedValue { get; }
}
