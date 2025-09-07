using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions;
/// <summary>
/// Defines a decorator that can wrap request handlers to provide cross-cutting concerns.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// Request decorators implement aspects such as logging, validation, caching,
/// authentication, authorization, and other cross-cutting concerns without
/// modifying the core business logic handlers.
/// </para>
/// <para>
/// Decorators are executed in order before the final handler, creating a
/// pipeline where each decorator can:
/// <list type="bullet">
///   <item><description>Execute logic before the next handler</description></item>
///   <item><description>Modify the request</description></item>
///   <item><description>Execute logic after the next handler</description></item>
///   <item><description>Modify or handle the response</description></item>
///   <item><description>Short-circuit the pipeline if needed</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class LoggingDecorator&lt;TRequest, TResponse&gt; : IRequestDecorator&lt;TRequest, TResponse&gt;
///   where TRequest : IRequest&lt;TResponse&gt; {
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
///     var stopwatch = Stopwatch.StartNew();
///     var requestName = typeof(TRequest).Name;
///     
///     _logger.LogInformation("Processing request: {RequestName}", requestName);
///     
///     try {
///       var result = await continuation();
///       
///       stopwatch.Stop();
///       _logger.LogInformation(
///         "Request {RequestName} completed in {ElapsedMs}ms with result: {IsSuccess}",
///         requestName, stopwatch.ElapsedMilliseconds, result.IsSuccess);
///       
///       return result;
///     } catch (Exception ex) {
///       stopwatch.Stop();
///       _logger.LogError(ex,
///         "Request {RequestName} failed after {ElapsedMs}ms",
///         requestName, stopwatch.ElapsedMilliseconds);
///       throw;
///     }
///   }
/// }
/// </code>
/// </example>
public interface IRequestDecorator<TRequest, TResponse>
  where TRequest : IRequest<TResponse> {

  /// <summary>
  /// Handles the request with the ability to execute logic before and after the next handler.
  /// </summary>
  /// <param name="request">The request to handle.</param>
  /// <param name="continuation">Function to invoke the next handler in the pipeline.</param>
  /// <param name="cancellationToken">
  /// A cancellation token that can be used to cancel the operation.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous operation.
  /// The task result contains a Result with either the response value or error information.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when request or continuation is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// Implementations should:
  /// <list type="bullet">
  ///   <item><description>Validate parameters for null</description></item>
  ///   <item><description>Respect the cancellation token</description></item>
  ///   <item><description>Call the continuation function to continue the pipeline</description></item>
  ///   <item><description>Handle exceptions appropriately</description></item>
  ///   <item><description>Avoid side effects that could break other decorators</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The decorator can choose to:
  /// <list type="bullet">
  ///   <item><description>Short-circuit by not calling continuation() and returning early</description></item>
  ///   <item><description>Modify the result returned from continuation()</description></item>
  ///   <item><description>Add error handling around the continuation() call</description></item>
  ///   <item><description>Add retry logic or other resilience patterns</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public Task<Result<TResponse>> HandleAsync(
    TRequest request,
    RequestHandlerFunction<TResponse> continuation,
    CancellationToken cancellationToken = default);
}
