using Microsoft.Extensions.Logging;
using NSI.Core.Results;

namespace NSI.AspNetCore.Results;

/// <summary>
/// High-performance logging extensions for the <see cref="Result{T}"/> pattern.
/// </summary>
/// <remarks>
/// <para>
/// Bridges domain result handling with structured logging via source-generated
/// <see cref="LoggerMessage"/> methods. Provides zero-allocation logging for the
/// common success, failure, and validation failure pathways.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Success => Information log (OperationSucceeded).</description></item>
///   <item><description>Failure (non-validation) => Warning log (OperationFailed) with code/message.</description></item>
///   <item><description>Validation failure => Warning log (ValidationFailed) with count.</description></item>
///   <item><description>No logging occurs when the logger is disabled for the target level.</description></item>
/// </list>
/// </para>
/// <para>EventId allocation (Identity / Core Result operations):
/// <list type="bullet">
///   <item><description>22: OperationSucceeded</description></item>
///   <item><description>23: OperationFailed</description></item>
///   <item><description>24: ValidationFailed</description></item>
/// </list>
/// Keep ids stable (contract for external log processors / dashboards).</para>
/// <para>Performance: Branching cost only; success path avoids boxing. Exception parameter
/// passed only when present. Validation count captured without LINQ.</para>
/// <para>Thread-safety: Stateless static API; safe for concurrent use.</para>
/// </remarks>
public static partial class ResultLoggingExtensions {
  /// <summary>
  /// Logs the supplied <paramref name="result"/> using semantic levels and returns it unchanged.
  /// </summary>
  /// <typeparam name="T">Result value type.</typeparam>
  /// <param name="result">Result instance.</param>
  /// <param name="logger">Target logger (must not be null).</param>
  /// <param name="operationName">Logical operation name (non-empty).</param>
  /// <returns>The same <paramref name="result"/> instance for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="logger"/> or <paramref name="operationName"/> is null.</exception>
  /// <exception cref="ArgumentException">When <paramref name="operationName"/> is blank.</exception>
  public static Result<T> LogResult<T>(this Result<T> result, ILogger logger, string operationName) {
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentNullException.ThrowIfNull(operationName);
    if (string.IsNullOrWhiteSpace(operationName)) {
      throw new ArgumentException("Operation name cannot be empty", nameof(operationName));
    }

    if (result.IsSuccess) {
      logger.LogOperationSucceeded(operationName, typeof(T).Name);
      return result;
    }

    var error = result.Error;
    if (error.HasValidationErrors) {
      var count = error.ValidationErrors?.Count ?? 0;
      logger.LogValidationFailed(operationName, count);
    } else {
      logger.LogOperationFailed(operationName, error.Code, error.Message, error.Exception);
    }
    return result;
  }

  /// <summary>Logs a successful operation returning a result type (EventId 22).</summary>
  /// <param name="logger">Logger.</param>
  /// <param name="operationName">Operation identifier.</param>
  /// <param name="resultType">Name of the returned result type.</param>
  [LoggerMessage(
    EventId = 22,
    EventName = "OperationSucceeded",
    Level = LogLevel.Information,
    Message = "Operation {OperationName} succeeded, returning {ResultType}")]
  public static partial void LogOperationSucceeded(this ILogger logger, string operationName, string resultType);

  /// <summary>Logs a non-validation failure (EventId 23).</summary>
  /// <param name="logger">Logger.</param>
  /// <param name="operationName">Operation identifier.</param>
  /// <param name="errorCode">Domain error code.</param>
  /// <param name="errorMessage">Readable error message.</param>
  /// <param name="exception">Optional associated exception.</param>
  [LoggerMessage(
    EventId = 23,
    EventName = "OperationFailed",
    Level = LogLevel.Warning,
    Message = "Operation {OperationName} failed with code {ErrorCode}: {ErrorMessage}")]
  public static partial void LogOperationFailed(
    this ILogger logger,
    string operationName,
    string errorCode,
    string errorMessage,
    Exception? exception);

  /// <summary>Logs a validation failure with number of validation errors (EventId 24).</summary>
  /// <param name="logger">Logger.</param>
  /// <param name="operationName">Operation identifier.</param>
  /// <param name="validationErrorCount">Count of validation errors.</param>
  [LoggerMessage(
    EventId = 24,
    EventName = "ValidationFailed",
    Level = LogLevel.Warning,
    Message = "Validation failed for operation {OperationName} with {ValidationErrorCount} errors")]
  public static partial void LogValidationFailed(
    this ILogger logger,
    string operationName,
    int validationErrorCount);
}
