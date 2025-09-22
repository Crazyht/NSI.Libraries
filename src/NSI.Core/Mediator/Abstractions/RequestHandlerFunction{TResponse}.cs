using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Represents the next step in the request processing pipeline.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <returns>
/// A task containing the <see cref="Result{T}"/> of the next step in the pipeline.
/// </returns>
/// <remarks>
/// <para>
/// This delegate is the core mechanism that enables the Decorator pattern in the mediator pipeline.
/// Each <see cref="IRequestDecorator{TRequest, TResponse}"/> receives this delegate to invoke 
/// the next step in the processing chain, allowing for comprehensive pre- and post-processing 
/// logic around the core <see cref="IRequestHandler{TRequest, TResponse}"/> execution.
/// </para>
/// <para>
/// Pipeline execution flow:
/// <list type="bullet">
///   <item><description>Decorator 1 (outermost): Logging, metrics, correlation</description></item>
///   <item><description>Decorator 2: Authentication and authorization</description></item>
///   <item><description>Decorator 3: Validation and business rules</description></item>
///   <item><description>Decorator N: Caching, retry logic, circuit breaker</description></item>
///   <item><description>Core Handler (innermost): Business logic execution</description></item>
/// </list>
/// </para>
/// <para>
/// The delegate encapsulates the continuation of the pipeline, allowing decorators to:
/// <list type="bullet">
///   <item><description>Execute logic before calling the continuation</description></item>
///   <item><description>Transform or validate the result after continuation</description></item>
///   <item><description>Short-circuit the pipeline by not calling the continuation</description></item>
///   <item><description>Add error handling, retry logic, or fallback mechanisms</description></item>
/// </list>
/// </para>
/// <para>
/// Performance note: The delegate is created once per request and passed down the pipeline,
/// avoiding closure allocations and ensuring optimal performance in high-throughput scenarios.
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
///   public async Task&lt;Result&lt;TResponse&gt;&gt; HandleAsync(
///     TRequest request, 
///     RequestHandlerFunction&lt;TResponse&gt; continuation, 
///     CancellationToken cancellationToken = default) {
///     
///     var stopwatch = Stopwatch.StartNew();
///     var requestName = typeof(TRequest).Name;
///     
///     // Pre-processing: Log request start with high-performance logging
///     _logger.LogRequestProcessingStarted(requestName);
///     
///     try {
///       // Call the next step in the pipeline
///       var result = await continuation();
///       
///       stopwatch.Stop();
///       
///       // Post-processing: Log completion with metrics
///       _logger.LogRequestProcessingCompleted(
///         requestName, 
///         stopwatch.ElapsedMilliseconds, 
///         result.IsSuccess);
///       
///       return result;
///     }
///     catch (Exception ex) when (ex is not OperationCanceledException) {
///       stopwatch.Stop();
///       
///       // Error handling: Log exception with context
///       _logger.LogRequestProcessingFailed(requestName, stopwatch.ElapsedMilliseconds, ex);
///       
///       // Re-throw to let outer decorators handle it
///       throw;
///     }
///   }
/// }
/// 
/// // Validation decorator that may short-circuit the pipeline
/// public class ValidationDecorator&lt;TRequest, TResponse&gt;: IRequestDecorator&lt;TRequest, TResponse&gt;
///   where TRequest: IRequest&lt;TResponse&gt; {
///   
///   public async Task&lt;Result&lt;TResponse&gt;&gt; HandleAsync(
///     TRequest request, 
///     RequestHandlerFunction&lt;TResponse&gt; continuation, 
///     CancellationToken cancellationToken = default) {
///     
///     // Pre-processing: Validate request
///     if (!IsValidRequest(request)) {
///       // Short-circuit: Return early without calling continuation
///       return Result.Failure&lt;TResponse&gt;(ResultError.Validation(
///         "INVALID_REQUEST", 
///         "Request validation failed"));
///     }
///     
///     // Continue pipeline only if validation passes
///     return await continuation();
///   }
/// }
/// 
/// // Caching decorator that may short-circuit on cache hit
/// public class CachingDecorator&lt;TRequest, TResponse&gt;: IRequestDecorator&lt;TRequest, TResponse&gt;
///   where TRequest: IRequest&lt;TResponse&gt; {
///   
///   public async Task&lt;Result&lt;TResponse&gt;&gt; HandleAsync(
///     TRequest request, 
///     RequestHandlerFunction&lt;TResponse&gt; continuation, 
///     CancellationToken cancellationToken = default) {
///     
///     var cacheKey = GenerateCacheKey(request);
///     
///     // Check cache first
///     if (await cache.TryGetAsync&lt;TResponse&gt;(cacheKey) is { } cachedValue) {
///       // Short-circuit: Return cached result without calling continuation
///       return Result.Success(cachedValue);
///     }
///     
///     // Cache miss: Execute pipeline and cache result
///     var result = await continuation();
///     
///     if (result.IsSuccess) {
///       await cache.SetAsync(cacheKey, result.Value, TimeSpan.FromMinutes(5));
///     }
///     
///     return result;
///   }
/// }
/// </code>
/// </example>
/// <seealso cref="IRequestDecorator{TRequest, TResponse}"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="Result{T}"/>
public delegate Task<Result<TResponse>> RequestHandlerFunction<TResponse>();
