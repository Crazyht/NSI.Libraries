using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Defines a decorator that can wrap request handlers to provide cross-cutting concerns.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// Request decorators implement the Decorator pattern to provide cross-cutting concerns
/// such as logging, validation, caching, authentication, authorization, performance monitoring,
/// and other aspects without modifying the core business logic handlers.
/// </para>
/// <para>
/// Decorators are executed in registration order before the final <see cref="IRequestHandler{TRequest, TResponse}"/>,
/// creating a pipeline where each decorator can:
/// <list type="bullet">
///   <item><description>Execute logic before the next handler (pre-processing)</description></item>
///   <item><description>Modify or validate the request</description></item>
///   <item><description>Execute logic after the next handler (post-processing)</description></item>
///   <item><description>Transform or enrich the response</description></item>
///   <item><description>Short-circuit the pipeline for caching or authorization</description></item>
///   <item><description>Add resilience patterns like retry or circuit breaker</description></item>
/// </list>
/// </para>
/// <para>
/// The decorator pattern integrates seamlessly with <see cref="IMediator"/> to provide
/// a flexible and composable architecture for handling cross-cutting concerns while
/// maintaining separation of concerns and testability.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // High-performance logging decorator using LoggerMessage
/// public class LoggingDecorator&lt;TRequest, TResponse&gt;: IRequestDecorator&lt;TRequest, TResponse&gt;
///   where TRequest: IRequest&lt;TResponse&gt; {
///   
///   private readonly ILogger&lt;LoggingDecorator&lt;TRequest, TResponse&gt;&gt; _logger;
///   
///   public LoggingDecorator(ILogger&lt;LoggingDecorator&lt;TRequest, TResponse&gt;&gt; logger) {
///     _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///   }
///   
///   public async Task&lt;Result&lt;TResponse&gt;&gt; HandleAsync(
///     TRequest request,
///     RequestHandlerFunction&lt;TResponse&gt; continuation,
///     CancellationToken cancellationToken = default) {
///     
///     ArgumentNullException.ThrowIfNull(request);
///     ArgumentNullException.ThrowIfNull(continuation);
///     
///     var stopwatch = Stopwatch.StartNew();
///     var requestName = typeof(TRequest).Name;
///     
///     _logger.LogProcessingStarted(requestName);
///     
///     try {
///       var result = await continuation();
///       
///       stopwatch.Stop();
///       _logger.LogProcessingCompleted(requestName, stopwatch.ElapsedMilliseconds, result.IsSuccess);
///       
///       return result;
///     }
///     catch (Exception ex) {
///       stopwatch.Stop();
///       _logger.LogProcessingFailed(requestName, stopwatch.ElapsedMilliseconds, ex);
///       throw;
///     }
///   }
/// }
/// 
/// // Validation decorator example
/// public class ValidationDecorator&lt;TRequest, TResponse&gt;: IRequestDecorator&lt;TRequest, TResponse&gt;
///   where TRequest: IRequest&lt;TResponse&gt; {
///   
///   private readonly IValidator&lt;TRequest&gt; _validator;
///   
///   public ValidationDecorator(IValidator&lt;TRequest&gt; validator) {
///     _validator = validator ?? throw new ArgumentNullException(nameof(validator));
///   }
///   
///   public async Task&lt;Result&lt;TResponse&gt;&gt; HandleAsync(
///     TRequest request,
///     RequestHandlerFunction&lt;TResponse&gt; continuation,
///     CancellationToken cancellationToken = default) {
///     
///     ArgumentNullException.ThrowIfNull(request);
///     ArgumentNullException.ThrowIfNull(continuation);
///     
///     var validationResult = await _validator.ValidateAsync(request, cancellationToken);
///     if (!validationResult.IsValid) {
///       return Result.Failure&lt;TResponse&gt;(ResultError.Validation(
///         "VALIDATION_FAILED", 
///         "Request validation failed", 
///         validationResult.Errors));
///     }
///     
///     return await continuation();
///   }
/// }
/// 
/// // Usage in dependency injection
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(ValidationDecorator&lt;,&gt;));
/// </code>
/// </example>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequest{TResponse}"/>
/// <seealso cref="Result{T}"/>
public interface IRequestDecorator<TRequest, TResponse>
  where TRequest: IRequest<TResponse> {

  /// <summary>
  /// Handles the request with the ability to execute logic before and after the next handler.
  /// </summary>
  /// <param name="request">The request to handle.</param>
  /// <param name="continuation">
  /// Function to invoke the next handler in the pipeline or the final <see cref="IRequestHandler{TRequest, TResponse}"/>.
  /// </param>
  /// <param name="cancellationToken">
  /// A cancellation token that can be used to cancel the operation.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous operation.
  /// The task result contains a <see cref="Result{T}"/> with either the response value or error information.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="request"/> or <paramref name="continuation"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// Implementations must:
  /// <list type="bullet">
  ///   <item><description>Validate parameters using <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/></description></item>
  ///   <item><description>Respect the <paramref name="cancellationToken"/> and pass it to async operations</description></item>
  ///   <item><description>Call <paramref name="continuation"/> to continue the pipeline unless short-circuiting</description></item>
  ///   <item><description>Handle exceptions appropriately without breaking the pipeline</description></item>
  ///   <item><description>Avoid side effects that could interfere with other decorators</description></item>
  ///   <item><description>Use high-performance patterns (LoggerMessage, static readonly fields, etc.)</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Advanced scenarios supported:
  /// <list type="bullet">
  ///   <item><description>Short-circuiting: Return early without calling continuation (e.g., for caching)</description></item>
  ///   <item><description>Result transformation: Modify the result returned from continuation</description></item>
  ///   <item><description>Error handling: Wrap continuation in try-catch for resilience patterns</description></item>
  ///   <item><description>Retry logic: Implement retry policies around the continuation call</description></item>
  ///   <item><description>Performance monitoring: Measure and log execution times</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Thread safety: Decorators should be stateless or use thread-safe patterns since they
  /// may be registered as singletons or scoped services in dependency injection containers.
  /// </para>
  /// </remarks>
  public Task<Result<TResponse>> HandleAsync(
    TRequest request,
    RequestHandlerFunction<TResponse> continuation,
    CancellationToken cancellationToken = default);
}
