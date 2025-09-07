using System.Collections.ObjectModel;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Results;

/// <summary>
/// Represents an error with a type, code, message, and optional exception or validation errors.
/// </summary>
/// <param name="Type">The categorized error type.</param>
/// <param name="Code">The error code for specific identification.</param>
/// <param name="Message">The error message describing the failure.</param>
/// <param name="Exception">The optional exception that caused the error.</param>
/// <param name="ValidationErrors">The optional list of validation errors for validation failures.</param>
/// <remarks>
/// <para>
/// This record struct provides a comprehensive way to represent errors in the Result pattern.
/// It includes type categorization, specific codes, human-readable messages, and support
/// for both exception interoperability and detailed validation error reporting.
/// </para>
/// <para>
/// The class provides static factory methods for creating common error types and
/// implicit conversion from string for convenience.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create typed errors
/// var notFoundError = ResultError.NotFound("USER_NOT_FOUND", "User with ID 123 was not found");
/// var authError = ResultError.Unauthorized("INVALID_TOKEN", "JWT token has expired");
/// 
/// // Create validation error
/// var validationErrors = new List&lt;IValidationError&gt; {
///   new ValidationError("Email", "Email is required", "REQUIRED"),
///   new ValidationError("Password", "Password must be at least 8 characters", "MIN_LENGTH")
/// };
/// var validationError = ResultError.Validation("INVALID_INPUT", "Validation failed", validationErrors);
/// 
/// // Implicit conversion from string
/// ResultError error = "Something went wrong";
/// </code>
/// </example>
public readonly record struct ResultError(
  ErrorType Type,
  string Code,
  string Message,
  Exception? Exception = null,
  IReadOnlyList<IValidationError>? ValidationErrors = null) {
  /// <summary>
  /// Gets a value indicating whether this error has validation errors.
  /// </summary>
  /// <value><c>true</c> if this error contains validation errors; otherwise, <c>false</c>.</value>
  public bool HasValidationErrors => ValidationErrors?.Count > 0;

  /// <summary>
  /// Gets a value indicating whether this error is of a specific type.
  /// </summary>
  /// <param name="errorType">The error type to check.</param>
  /// <returns><c>true</c> if the error is of the specified type; otherwise, <c>false</c>.</returns>
  public bool IsOfType(ErrorType errorType) => Type == errorType;

  /// <summary>
  /// Creates a validation error with a list of validation failures.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="validationErrors">The list of validation errors.</param>
  /// <returns>A ResultError of type Validation.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <exception cref="ArgumentNullException">Thrown when validationErrors is null.</exception>
  /// <example>
  /// <code>
  /// var errors = new List&lt;IValidationError&gt; {
  ///   new ValidationError("Email", "Email is required", "REQUIRED")
  /// };
  /// var validationError = ResultError.Validation("INVALID_USER", "User validation failed", errors);
  /// </code>
  /// </example>
  public static ResultError Validation(string code, string message, IEnumerable<IValidationError> validationErrors) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    ArgumentNullException.ThrowIfNull(validationErrors);

    var errorList = validationErrors.ToList().AsReadOnly();
    return new ResultError(ErrorType.Validation, code, message, ValidationErrors: errorList);
  }

  /// <summary>
  /// Creates a validation error with a single validation failure.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="validationError">The validation error.</param>
  /// <returns>A ResultError of type Validation.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <exception cref="ArgumentNullException">Thrown when validationError is null.</exception>
  /// <example>
  /// <code>
  /// var error = new ValidationError("Email", "Email is required", "REQUIRED");
  /// var validationError = ResultError.Validation("INVALID_EMAIL", "Email validation failed", error);
  /// </code>
  /// </example>
  public static ResultError Validation(string code, string message, IValidationError validationError) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    ArgumentNullException.ThrowIfNull(validationError);

    return new ResultError(ErrorType.Validation, code, message,
      ValidationErrors: new ReadOnlyCollection<IValidationError>([validationError]));
  }

  /// <summary>
  /// Creates an authentication error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type Authentication.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var authError = ResultError.Unauthorized("INVALID_CREDENTIALS", "Username or password is incorrect");
  /// </code>
  /// </example>
  public static ResultError Unauthorized(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.Authentication, code, message, exception);
  }

  /// <summary>
  /// Creates an authorization error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type Authorization.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var forbiddenError = ResultError.Forbidden("INSUFFICIENT_PERMISSIONS", "User lacks required permissions");
  /// </code>
  /// </example>
  public static ResultError Forbidden(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.Authorization, code, message, exception);
  }

  /// <summary>
  /// Creates a not found error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type NotFound.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var notFoundError = ResultError.NotFound("USER_NOT_FOUND", "User with ID 123 was not found");
  /// </code>
  /// </example>
  public static ResultError NotFound(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.NotFound, code, message, exception);
  }

  /// <summary>
  /// Creates a conflict error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type Conflict.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var conflictError = ResultError.Conflict("DUPLICATE_EMAIL", "Email address is already registered");
  /// </code>
  /// </example>
  public static ResultError Conflict(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.Conflict, code, message, exception);
  }

  /// <summary>
  /// Creates a service unavailable error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type ServiceUnavailable.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var serviceError = ResultError.ServiceUnavailable("API_DOWN", "External API is currently unavailable");
  /// </code>
  /// </example>
  public static ResultError ServiceUnavailable(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.ServiceUnavailable, code, message, exception);
  }

  /// <summary>
  /// Creates a business rule violation error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type BusinessRule.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var businessError = ResultError.BusinessRule("INSUFFICIENT_BALANCE", "Account balance is insufficient for this transaction");
  /// </code>
  /// </example>
  public static ResultError BusinessRule(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.BusinessRule, code, message, exception);
  }

  /// <summary>
  /// Creates a database error.
  /// </summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <param name="exception">The optional exception.</param>
  /// <returns>A ResultError of type Database.</returns>
  /// <exception cref="ArgumentException">Thrown when code or message is null or empty.</exception>
  /// <example>
  /// <code>
  /// var dbError = ResultError.Database("CONNECTION_FAILED", "Unable to connect to database", sqlException);
  /// </code>
  /// </example>
  public static ResultError Database(string code, string message, Exception? exception = null) {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
    ArgumentException.ThrowIfNullOrWhiteSpace(message);

    return new ResultError(ErrorType.Database, code, message, exception);
  }

  /// <summary>
  /// Returns a string representation of the error.
  /// </summary>
  /// <returns>A formatted string containing the error type, code, and message.</returns>
  public override string ToString() => $"[{Type}:{Code}] {Message}";

  /// <summary>
  /// Creates a standardized validation error for a missing request body.
  /// </summary>
  /// <returns>
  /// A <see cref="ResultError"/> representing a null request body validation error.
  /// </returns>
  /// <example>
  /// <code>
  /// if (request == null) {
  ///   return ResultError.NullRequest().ToProblemDetails();
  /// }
  /// </code>
  /// </example>
  public static ResultError BodyNullRequest()
    => Validation(
      "NULL_REQUEST",
      "Request body is required",
      new ValidationError("Request", "NULL", "Request body cannot be null")
    );
}
