using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation;

/// <summary>
/// Immutable value semantic representing a single validation failure.
/// </summary>
/// <remarks>
/// <para>
/// Supplies structured detail (member path, machine code, human message, comparator value) enabling
/// consistent UI feedback, localization mapping, problem details projections, and analytics.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="PropertyName"/> may be null for object-level / aggregate rules.</description></item>
///   <item><description><see cref="ErrorCode"/> is a stable UPPER_SNAKE_CASE token (do not localize).</description></item>
///   <item><description><see cref="ErrorMessage"/> is baseline English text (localizers map by code).</description></item>
///   <item><description><see cref="ExpectedValue"/> conveys constraint data (e.g. max length, pattern) or null.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Keep messages concise (&lt;= 120 chars) and free of PII.</description></item>
///   <item><description>Use dot notation for nested members (e.g. <c>User.Address.City</c>).</description></item>
///   <item><description>Do not change published <see cref="ErrorCode"/> values (backwards compatibility).</description></item>
///   <item><description>Leave <see cref="ExpectedValue"/> null if no comparator meaningfully applies.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Struct is immutable; safe to share across threads.</para>
/// <para>Performance: Tiny value type; passed by readonly reference when large enumerations are involved.
/// Avoid boxing by consuming via <see cref="IValidationError"/> only when necessary.</para>
/// </remarks>
/// <example>
/// <code>
/// var error = new ValidationError("REQUIRED", "Email is required.", "Email");
/// Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage} ({error.ErrorCode})");
/// </code>
/// </example>
public readonly record struct ValidationError: IValidationError {
  /// <inheritdoc />
  public string? PropertyName { get; }

  /// <inheritdoc />
  public string ErrorCode { get; }

  /// <inheritdoc />
  public string ErrorMessage { get; }

  /// <inheritdoc />
  public object? ExpectedValue { get; }

  /// <summary>
  /// Initializes a new validation error instance.
  /// </summary>
  /// <param name="errorCode">Stable uppercase identifier (e.g. REQUIRED, TOO_LONG).</param>
  /// <param name="errorMessage">Human-readable baseline English description.</param>
  /// <param name="propertyName">Logical member path (dot notation) or null.</param>
  /// <param name="expectedValue">Optional comparator / constraint value.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="errorCode"/> or <paramref name="errorMessage"/> is null/whitespace.</exception>
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
