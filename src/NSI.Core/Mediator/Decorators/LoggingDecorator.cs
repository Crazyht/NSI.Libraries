using System.Diagnostics;
using System.Globalization;

using Microsoft.Extensions.Logging;

using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;

namespace NSI.Core.Mediator.Decorators;
/// <summary>
/// Decorator that adds comprehensive logging around request processing.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// This decorator provides structured logging with timing information,
/// request/response details, and error handling. It uses correlation IDs
/// to track requests across the application.
/// </para>
/// <para>
/// Logged information includes:
/// <list type="bullet">
///   <item><description>Request start with type and correlation ID</description></item>
///   <item><description>Execution time measurement</description></item>
///   <item><description>Success/failure status</description></item>
///   <item><description>Error details for failed requests</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration in DI
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
/// 
/// // Logged output example:
/// // [INFO] Starting request processing: GetUserByIdQuery (CorrelationId: abc123)
/// // [INFO] Request GetUserByIdQuery completed successfully in 150ms
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the <see cref="LoggingDecorator{TRequest, TResponse}"/> class.
/// </remarks>
/// <param name="logger">The logger for diagnostic information.</param>
/// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
public class LoggingDecorator<TRequest, TResponse>(ILogger<LoggingDecorator<TRequest, TResponse>> logger): IRequestDecorator<TRequest, TResponse>
  where TRequest : IRequest<TResponse> {

  private readonly ILogger<LoggingDecorator<TRequest, TResponse>> _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

  /// <summary>
  /// Handles the request with comprehensive logging around the execution.
  /// </summary>
  /// <param name="request">The request to handle.</param>
  /// <param name="continuation">Function to invoke the next handler in the pipeline.</param>
  /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
  /// <returns>A task that represents the asynchronous operation containing the result.</returns>
  /// <exception cref="ArgumentNullException">Thrown when request or continuation is null.</exception>
  public async Task<Result<TResponse>> HandleAsync(
    TRequest request,
    RequestHandlerFunction<TResponse> continuation,
    CancellationToken cancellationToken = default) {

    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(continuation);

    var requestName = typeof(TRequest).Name;
    var correlationId = GetCorrelationId(request);
    var stopwatch = Stopwatch.StartNew();

    using var scope = _Logger.BeginScope(new Dictionary<string, object?> {
      ["RequestType"] = requestName,
      ["CorrelationId"] = correlationId
    });

    _Logger.LogDecoratorRequestStarting(requestName, correlationId);

    try {
      var result = await continuation();

      stopwatch.Stop();

      if (result.IsSuccess) {
        _Logger.LogDecoratorRequestCompleted(requestName, stopwatch.ElapsedMilliseconds);
      } else {
        _Logger.LogDecoratorRequestFailed(
          requestName,
          stopwatch.ElapsedMilliseconds,
          result.Error.Type,
          result.Error.Code,
          result.Error.Message);
      }

      return result;
    } catch (OperationCanceledException) {
      stopwatch.Stop();
      _Logger.LogDecoratorRequestCancelled(requestName, stopwatch.ElapsedMilliseconds);
      throw;
    } catch (Exception ex) {
      stopwatch.Stop();
      _Logger.LogDecoratorRequestException(requestName, stopwatch.ElapsedMilliseconds, ex);
      throw;
    }
  }

  /// <summary>
  /// Short correlation ID length for readability.
  /// </summary>
  private const int CorrelationIdLength = 8;

  /// <summary>
  /// Extracts or generates a correlation ID for the request.
  /// </summary>
  /// <param name="request">The request to extract correlation ID from.</param>
  /// <returns>The correlation ID as a string.</returns>
  /// <remarks>
  /// If the request implements ICorrelatedRequest, uses its CorrelationId.
  /// Otherwise, generates a new GUID for tracking purposes.
  /// </remarks>
  private static string GetCorrelationId(TRequest request) {
    if (request is ICorrelatedRequest correlated && !string.IsNullOrEmpty(correlated.CorrelationId)) {
      return correlated.CorrelationId;
    }

    return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..CorrelationIdLength]; // Short correlation ID for readability
  }
}

/// <summary>
/// Interface for requests that support correlation tracking.
/// </summary>
/// <remarks>
/// Implement this interface on request types that need to maintain
/// correlation across multiple operations or external service calls.
/// </remarks>
public interface ICorrelatedRequest {
  /// <summary>
  /// Gets the correlation identifier for tracking this request across operations.
  /// </summary>
  /// <value>A unique identifier for correlation tracking.</value>
  public string? CorrelationId { get; }
}
