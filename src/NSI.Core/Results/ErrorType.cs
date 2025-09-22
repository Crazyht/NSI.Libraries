namespace NSI.Core.Results;

/// <summary>
/// Defines common error types for Result pattern operations.
/// </summary>
/// <remarks>
/// This enumeration provides standardized error categorization for business applications.
/// Each error type maps to specific HTTP status codes for consistent API responses.
/// </remarks>
/// <seealso cref="Result{T}"/>
/// <seealso cref="ResultError"/>
public enum ErrorType {

  /// <summary>
  /// Generic error type for uncategorized failures or system-level errors.
  /// Maps to HTTP 500 Internal Server Error.
  /// </summary>
  Generic,

  /// <summary>
  /// Validation errors for input validation failures.
  /// Maps to HTTP 400 Bad Request.
  /// </summary>
  Validation,

  /// <summary>
  /// Authentication errors for user identity verification failures.
  /// Maps to HTTP 401 Unauthorized.
  /// </summary>
  Authentication,

  /// <summary>
  /// Authorization errors for insufficient permissions.
  /// Maps to HTTP 403 Forbidden.
  /// </summary>
  Authorization,

  /// <summary>
  /// Resource not found errors for missing entities.
  /// Maps to HTTP 404 Not Found.
  /// </summary>
  NotFound,

  /// <summary>
  /// Conflict errors for duplicate resources and state conflicts.
  /// Maps to HTTP 409 Conflict.
  /// </summary>
  Conflict,

  /// <summary>
  /// External service or system unavailable errors.
  /// Maps to HTTP 503 Service Unavailable.
  /// </summary>
  ServiceUnavailable,

  /// <summary>
  /// Database or data access related errors.
  /// Maps to HTTP 500 Internal Server Error.
  /// </summary>
  Database,

  /// <summary>
  /// Network or communication related errors.
  /// Maps to HTTP 502 Bad Gateway or 504 Gateway Timeout.
  /// </summary>
  Network,

  /// <summary>
  /// Business rule violation errors for domain-specific constraints.
  /// Maps to HTTP 422 Unprocessable Entity.
  /// </summary>
  BusinessRule,

  /// <summary>
  /// Rate limiting or throttling errors.
  /// Maps to HTTP 429 Too Many Requests.
  /// </summary>
  RateLimit,

  /// <summary>
  /// Timeout errors for operations that exceed time limits.
  /// Maps to HTTP 504 Gateway Timeout.
  /// </summary>
  Timeout
}
