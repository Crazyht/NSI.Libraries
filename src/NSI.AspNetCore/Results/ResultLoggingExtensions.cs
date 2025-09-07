using Microsoft.Extensions.Logging;
using NSI.Core.Results;

namespace NSI.AspNetCore.Results {
  /// <summary>
  /// Provides extension methods for logging Result operations.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These extensions integrate Result pattern with structured logging using
  /// LoggerMessage source generators for optimal performance.
  /// </para>
  /// </remarks>
  public static partial class ResultLoggingExtensions {
    /// <summary>
    /// Logs the result of an operation, with different log levels based on success/failure.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The name of the operation for logging context.</param>
    /// <returns>The same result instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// var result = await userService.GetUserByIdAsync(userId);
    /// return result
    ///   .LogResult(_logger, "GetUserById")
    ///   .ToHttpResponse();
    /// </code>
    /// </example>
    public static Result<T> LogResult<T>(this Result<T> result, ILogger logger, string operationName) {
      if (result.IsSuccess) {
        logger.LogOperationSucceeded(operationName, typeof(T).Name);
      } else {
        if (result.Error.HasValidationErrors) {
          logger.LogValidationFailed(operationName, result.Error.ValidationErrors!.Count);
        } else {
          logger.LogOperationFailed(operationName, result.Error.Code, result.Error.Message, result.Error.Exception);
        }
      }

      return result;
    }

    [LoggerMessage(
      EventId = 2001,
      EventName = "OperationSucceeded",
      Level = LogLevel.Information,
      Message = "Operation {OperationName} succeeded, returning {ResultType}"
    )]
    public static partial void LogOperationSucceeded(this ILogger logger, string operationName, string resultType);

    [LoggerMessage(
      EventId = 2002,
      EventName = "OperationFailed",
      Level = LogLevel.Warning,
      Message = "Operation {OperationName} failed with code {ErrorCode}: {ErrorMessage}"
    )]
    public static partial void LogOperationFailed(this ILogger logger, string operationName, string errorCode, string errorMessage, Exception? exception);

    [LoggerMessage(
      EventId = 2003,
      EventName = "ValidationFailed",
      Level = LogLevel.Warning,
      Message = "Validation failed for operation {OperationName} with {ValidationErrorCount} errors"
    )]
    public static partial void LogValidationFailed(this ILogger logger, string operationName, int validationErrorCount);
  }
}
