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
/// This decorator provides high-performance structured logging with timing information,
/// request/response details, and error handling using LoggerMessage source generators.
/// It uses correlation IDs to track requests across the application and integrates
/// seamlessly with the decorator pipeline.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Zero-allocation logging using LoggerMessage source generators</description></item>
///   <item><description>Automatic correlation ID generation and tracking</description></item>
///   <item><description>Precise execution time measurement with Stopwatch</description></item>
///   <item><description>Structured logging with scoped context</description></item>
///   <item><description>Comprehensive error and cancellation handling</description></item>
///   <item><description>Result pattern integration for business logic failures</description></item>
/// </list>
/// </para>
/// <para>
/// The decorator automatically handles different request outcomes:
/// <list type="bullet">
///   <item><description>Success: Logs completion with timing and success status</description></item>
///   <item><description>Business failures: Logs Result failures with error details</description></item>
///   <item><description>Cancellation: Logs operation cancellation with timing</description></item>
///   <item><description>Exceptions: Logs infrastructure exceptions with full context</description></item>
/// </list>
/// </para>
/// <para>
/// Performance considerations: Uses LoggerMessage source generators for optimal performance,
/// scoped logging for structured data, and minimal string allocations. The correlation ID
/// is truncated to 8 characters for readability while maintaining uniqueness in typical scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration in dependency injection
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
/// 
/// // Example logged output:
/// // [INFO] Request processing started: GetUserByIdQuery (CorrelationId: a1b2c3d4)
/// // [INFO] Request GetUserByIdQuery completed successfully in 150ms
/// 
/// // For requests with correlation IDs:
/// public record GetUserQuery(Guid UserId, string? CorrelationId = null): IQuery&lt;User&gt;, ICorrelatedRequest;
/// 
/// // Usage with custom correlation:
/// var query = new GetUserQuery(userId, "custom-correlation-123");
/// var result = await mediator.ProcessAsync(query, cancellationToken);
/// </code>
/// </example>
/// <seealso cref="IRequestDecorator{TRequest, TResponse}"/>
/// <seealso cref="ICorrelatedRequest"/>
/// <seealso cref="RequestHandlerFunction{TResponse}"/>
/// <seealso cref="Result{T}"/>
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
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> or <paramref name="continuation"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// The method follows these steps:
  /// <list type="number">
  ///   <item><description>Validates input parameters</description></item>
  ///   <item><description>Extracts or generates correlation ID</description></item>
  ///   <item><description>Creates logging scope with structured data</description></item>
  ///   <item><description>Logs request start with high-performance LoggerMessage</description></item>
  ///   <item><description>Executes the continuation function</description></item>
  ///   <item><description>Logs appropriate completion, failure, or exception message</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Exception handling strategy:
  /// <list type="bullet">
  ///   <item><description>OperationCanceledException: Logged and re-thrown for proper cancellation handling</description></item>
  ///   <item><description>Other exceptions: Logged with full context and re-thrown for upstream handling</description></item>
  ///   <item><description>Result failures: Logged with error details but not thrown (business logic failures)</description></item>
  /// </list>
  /// </para>
  /// </remarks>
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
  /// <para>
  /// Correlation ID resolution strategy:
  /// <list type="number">
  ///   <item><description>If request implements <see cref="ICorrelatedRequest"/> and has a non-empty CorrelationId, use it</description></item>
  ///   <item><description>Otherwise, generate a new GUID and truncate to <see cref="CorrelationIdLength"/> characters</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The truncation provides a balance between uniqueness and readability in logs.
  /// In typical applications, 8 hexadecimal characters provide sufficient uniqueness
  /// for correlation within a reasonable time window.
  /// </para>
  /// </remarks>
  private static string GetCorrelationId(TRequest request) {
    if (request is ICorrelatedRequest correlated && !string.IsNullOrEmpty(correlated.CorrelationId)) {
      return correlated.CorrelationId;
    }

    return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..CorrelationIdLength];
  }
}

/// <summary>
/// Interface for requests that support correlation tracking.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on request types that need to maintain correlation across
/// multiple operations, external service calls, or distributed transactions. The correlation
/// ID enables tracing requests through complex processing pipelines and across service boundaries.
/// </para>
/// <para>
/// Common usage patterns:
/// <list type="bullet">
///   <item><description>HTTP requests: Propagate correlation IDs from HTTP headers</description></item>
///   <item><description>Message queues: Maintain correlation across async message processing</description></item>
///   <item><description>Distributed tracing: Integration with OpenTelemetry or similar systems</description></item>
///   <item><description>Business transactions: Track multi-step business processes</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple correlated query
/// public record GetUserQuery(Guid UserId, string? CorrelationId = null): IQuery&lt;User&gt;, ICorrelatedRequest;
/// 
/// // Complex correlated command
/// public record ProcessPaymentCommand(
///   decimal Amount, 
///   string Currency, 
///   string? CorrelationId = null): ICommand&lt;PaymentResult&gt;, ICorrelatedRequest;
/// 
/// // Usage with HTTP context
/// var correlationId = HttpContext.TraceIdentifier;
/// var command = new ProcessPaymentCommand(100.00m, "USD", correlationId);
/// var result = await mediator.ProcessAsync(command, cancellationToken);
/// </code>
/// </example>
/// <seealso cref="LoggingDecorator{TRequest, TResponse}"/>
/// <seealso cref="IRequest{TResponse}"/>
public interface ICorrelatedRequest {
  /// <summary>
  /// Gets the correlation identifier for tracking this request across operations.
  /// </summary>
  /// <value>
  /// A unique identifier for correlation tracking, or null if no specific correlation is needed.
  /// </value>
  /// <remarks>
  /// <para>
  /// The correlation ID should be:
  /// <list type="bullet">
  ///   <item><description>Unique within the correlation context (e.g., per HTTP request)</description></item>
  ///   <item><description>Consistent across related operations</description></item>
  ///   <item><description>Suitable for logging and tracing systems</description></item>
  ///   <item><description>Reasonably short for readability (recommended 8-36 characters)</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// When null or empty, the <see cref="LoggingDecorator{TRequest, TResponse}"/> will
  /// automatically generate a correlation ID for tracking purposes.
  /// </para>
  /// </remarks>
  public string? CorrelationId { get; }
}
