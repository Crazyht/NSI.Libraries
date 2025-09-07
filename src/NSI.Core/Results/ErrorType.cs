namespace NSI.Core.Results {
  /// <summary>
  /// Defines common error types for Result pattern operations.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This enumeration provides a standardized way to categorize common types of errors
  /// that occur in business applications. It helps in error handling and provides
  /// semantic meaning to different failure scenarios.
  /// </para>
  /// </remarks>
  public enum ErrorType {
    /// <summary>
    /// Generic error type for uncategorized failures.
    /// </summary>
    Generic,

    /// <summary>
    /// Validation errors, typically input validation failures.
    /// </summary>
    Validation,

    /// <summary>
    /// Authentication errors, user identity verification failures.
    /// </summary>
    Authentication,

    /// <summary>
    /// Authorization errors, insufficient permissions.
    /// </summary>
    Authorization,

    /// <summary>
    /// Resource not found errors.
    /// </summary>
    NotFound,

    /// <summary>
    /// Conflict errors, typically when trying to create duplicate resources.
    /// </summary>
    Conflict,

    /// <summary>
    /// External service or system unavailable.
    /// </summary>
    ServiceUnavailable,

    /// <summary>
    /// Database or data access related errors.
    /// </summary>
    Database,

    /// <summary>
    /// Network or communication related errors.
    /// </summary>
    Network,

    /// <summary>
    /// Business rule violation errors.
    /// </summary>
    BusinessRule,

    /// <summary>
    /// Rate limiting or throttling errors.
    /// </summary>
    RateLimit,

    /// <summary>
    /// Timeout errors for operations that exceed time limits.
    /// </summary>
    Timeout
  }
}
