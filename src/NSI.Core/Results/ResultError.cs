using System.Collections.ObjectModel;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Results;

/// <summary>
/// Represents a categorized error (type, code, message) with optional exception and validation details.
/// </summary>
/// <remarks>
/// <para>
/// Used by the Result pattern to carry structured failure information without relying solely on exceptions.
/// Each <see cref="ErrorType"/> maps to standardized handling (e.g. HTTP status translation, logging strategy).
/// </para>
/// <para>
/// Design goals:
/// <list type="bullet">
///   <item><description>Structured categorization via <see cref="Type"/></description></item>
///   <item><description>Stable machine code via <see cref="Code"/> (UPPER_SNAKE_CASE)</description></item>
///   <item><description>User / developer readable <see cref="Message"/></description></item>
///   <item><description>Optional root <see cref="Exception"/> (never required)</description></item>
///   <item><description>Optional field-level <see cref="ValidationErrors"/> list for validation failures</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: This is an immutable value type. All members are safe for concurrent read access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var nf = ResultError.NotFound("USER_NOT_FOUND", "User 42 not found");
/// var validation = ResultError.Validation(
///   "INVALID_USER", "User validation failed",
///   new [] { new ValidationError("Email", "REQUIRED", "Email is required") });
/// ResultError generic = "Unexpected failure"; // implicit from string
/// </code>
/// </example>
public readonly record struct ResultError {
  /// <summary>Gets the categorized error type.</summary>
  public ErrorType Type { get; }
  /// <summary>Gets the stable machine-readable error code (UPPER_SNAKE_CASE).</summary>
  public string Code { get; }
  /// <summary>Gets the human-readable error message.</summary>
  public string Message { get; }
  /// <summary>Gets the originating exception when present (optional).</summary>
  public Exception? Exception { get; }
  /// <summary>Gets the validation errors when <see cref="Type"/> is Validation.</summary>
  public IReadOnlyList<IValidationError>? ValidationErrors { get; }

  /// <summary>Initializes a new error instance (prefer factories for clarity).</summary>
  /// <param name="type">Categorized error type.</param>
  /// <param name="code">Stable UPPER_SNAKE_CASE code.</param>
  /// <param name="message">Human-readable description.</param>
  /// <param name="exception">Optional root exception.</param>
  /// <param name="validationErrors">Optional validation errors list.</param>
  public ResultError(
    ErrorType type,
    string code,
    string message,
    Exception? exception = null,
    IReadOnlyList<IValidationError>? validationErrors = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    Type = type;
    Code = code;
    Message = message;
    Exception = exception;
    ValidationErrors = validationErrors;
  }

  /// <summary>Gets a value indicating whether validation errors are attached.</summary>
  public bool HasValidationErrors => ValidationErrors?.Count > 0;
  /// <summary>Gets a value indicating whether an exception is attached.</summary>
  public bool HasException => Exception is not null;
  /// <summary>Returns true if this error matches the provided type.</summary>
  /// <param name="errorType">Error type to compare.</param>
  public bool IsOfType(ErrorType errorType) => Type == errorType;

  /// <summary>Creates a validation error with multiple validation failures.</summary>
  /// <param name="code">Stable error code.</param>
  /// <param name="message">Human-readable message.</param>
  /// <param name="validationErrors">Validation failures.</param>
  /// <returns>A validation <see cref="ResultError"/> instance.</returns>
  public static ResultError Validation(
    string code,
    string message,
    IEnumerable<IValidationError> validationErrors) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    ArgumentNullException.ThrowIfNull(validationErrors);
    var list = validationErrors as IReadOnlyList<IValidationError> ?? [.. validationErrors];
    return new ResultError(ErrorType.Validation, code, message, validationErrors: list);
  }

  /// <summary>Creates a validation error with a single validation failure.</summary>
  /// <param name="code">Stable error code.</param>
  /// <param name="message">Human-readable message.</param>
  /// <param name="validationError">Single validation failure.</param>
  public static ResultError Validation(
    string code,
    string message,
    IValidationError validationError) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    ArgumentNullException.ThrowIfNull(validationError);
    return new ResultError(
      ErrorType.Validation,
      code,
      message,
      validationErrors: new ReadOnlyCollection<IValidationError>([validationError]));
  }

  /// <summary>Creates an authentication (401) error.</summary>
  public static ResultError Unauthorized(string code, string message, Exception? exception = null)
    => new(ErrorType.Authentication, code, message, exception);
  /// <summary>Creates an authorization (403) error.</summary>
  public static ResultError Forbidden(string code, string message, Exception? exception = null)
    => new(ErrorType.Authorization, code, message, exception);
  /// <summary>Creates a not found (404) error.</summary>
  public static ResultError NotFound(string code, string message, Exception? exception = null)
    => new(ErrorType.NotFound, code, message, exception);
  /// <summary>Creates a conflict (409) error.</summary>
  public static ResultError Conflict(string code, string message, Exception? exception = null)
    => new(ErrorType.Conflict, code, message, exception);
  /// <summary>Creates a business rule (422) error.</summary>
  public static ResultError BusinessRule(string code, string message, Exception? exception = null)
    => new(ErrorType.BusinessRule, code, message, exception);

  /// <summary>Creates a service unavailable (503) error.</summary>
  public static ResultError ServiceUnavailable(string code, string message, Exception? exception = null)
    => new(ErrorType.ServiceUnavailable, code, message, exception);
  /// <summary>Creates a database (500) error.</summary>
  public static ResultError Database(string code, string message, Exception? exception = null)
    => new(ErrorType.Database, code, message, exception);
  /// <summary>Creates a network (502/504) error.</summary>
  public static ResultError Network(string code, string message, Exception? exception = null)
    => new(ErrorType.Network, code, message, exception);
  /// <summary>Creates a timeout (504) error.</summary>
  public static ResultError Timeout(string code, string message, Exception? exception = null)
    => new(ErrorType.Timeout, code, message, exception);
  /// <summary>Creates a rate limit (429) error.</summary>
  public static ResultError RateLimit(string code, string message, Exception? exception = null)
    => new(ErrorType.RateLimit, code, message, exception);
  /// <summary>Creates a generic (500) error.</summary>
  public static ResultError Generic(string code, string message, Exception? exception = null)
    => new(ErrorType.Generic, code, message, exception);

  /// <summary>Standardized validation error for a null request body.</summary>
  public static ResultError BodyNullRequest() => Validation(
    "NULL_REQUEST",
    "Request body is required",
    new ValidationError("Request", "NULL", "Request body cannot be null")
  );

  /// <summary>Implicit conversion from string to a generic error (code = GENERIC).</summary>
  /// <param name="message">Error message.</param>
  public static implicit operator ResultError(string message) {
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    return new ResultError(ErrorType.Generic, "GENERIC", message);
  }

  /// <inheritdoc />
  public override string ToString() => $"[{Type}:{Code}] {Message}";
}
