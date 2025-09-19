using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;

namespace NSI.Core.Mediator;

/// <summary>
/// Default implementation of the mediator pattern for handling requests and notifications.
/// </summary>
/// <remarks>
/// <para>
/// This mediator implementation provides request/response handling with support for
/// cross-cutting concerns through decorators. It uses dependency injection to resolve
/// handlers and maintains a cache of compiled delegates for optimal performance.
/// </para>
/// <para>
/// Key architectural features:
/// <list type="bullet">
///   <item><description>Automatic handler resolution via dependency injection container</description></item>
///   <item><description>Type-safe request/response handling with compile-time validation</description></item>
///   <item><description>Fire-and-forget notification dispatching with parallel execution</description></item>
///   <item><description>Decorator pipeline for cross-cutting concerns (logging, validation, caching)</description></item>
///   <item><description>High-performance compiled delegates with static caching</description></item>
///   <item><description>Comprehensive error handling with Result pattern integration</description></item>
///   <item><description>Thread-safe concurrent operations with lock-free data structures</description></item>
/// </list>
/// </para>
/// <para>
/// Performance optimizations:
/// <list type="bullet">
///   <item><description>Static delegate caching: Compiled expressions cached per request/response type combination</description></item>
///   <item><description>Type resolution caching: Handler and decorator types cached to avoid reflection overhead</description></item>
///   <item><description>Concurrent collections: Thread-safe caches using ConcurrentDictionary for high-throughput scenarios</description></item>
///   <item><description>Zero-allocation logging: High-performance LoggerMessage source generators used throughout</description></item>
///   <item><description>Pipeline optimization: Minimal overhead decorator chains with compiled delegates</description></item>
/// </list>
/// </para>
/// <para>
/// The mediator is thread-safe and can be registered as a singleton or scoped service
/// in the dependency injection container. For high-throughput applications, singleton
/// registration is recommended to maximize caching benefits.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration in dependency injection container
/// services.AddScoped&lt;IMediator, MediatorImplementation&gt;();
/// 
/// // Or register as singleton for better performance
/// services.AddSingleton&lt;IMediator, MediatorImplementation&gt;();
/// 
/// // Usage in application services
/// public class UserService {
///   private readonly IMediator _mediator;
///   
///   public UserService(IMediator mediator) {
///     _mediator = mediator;
///   }
///   
///   public async Task&lt;Result&lt;User&gt;&gt; GetUserAsync(Guid userId, CancellationToken cancellationToken) {
///     var query = new GetUserByIdQuery(userId);
///     return await _mediator.ProcessAsync(query, cancellationToken);
///   }
///   
///   public async Task&lt;Result&lt;User&gt;&gt; CreateUserAsync(string email, string name, CancellationToken cancellationToken) {
///     var command = new CreateUserCommand(email, name);
///     var result = await _mediator.ProcessAsync(command, cancellationToken);
///     
///     if (result.IsSuccess) {
///       // Fire notification without waiting
///       await _mediator.DispatchAsync(new UserCreatedNotification(result.Value.Id, email));
///     }
///     
///     return result;
///   }
/// }
/// 
/// // Handler registration example
/// services.AddScoped&lt;IRequestHandler&lt;GetUserByIdQuery, User&gt;, GetUserByIdQueryHandler&gt;();
/// services.AddScoped&lt;IRequestHandler&lt;CreateUserCommand, User&gt;, CreateUserCommandHandler&gt;();
/// 
/// // Decorator registration for cross-cutting concerns
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(ValidationDecorator&lt;,&gt;));
/// </code>
/// </example>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="IRequestDecorator{TRequest, TResponse}"/>
/// <seealso cref="Result{T}"/>
/// <seealso cref="ResultError"/>
public class MediatorImplementation(
  IServiceProvider serviceProvider,
  ILogger<MediatorImplementation> logger): IMediator {

  private readonly IServiceProvider _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
  private readonly ILogger<MediatorImplementation> _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

  // Thread-safe caches for compiled delegates and types to avoid reflection overhead
  private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> HandlerTypeCache = new();
  private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> DecoratorTypeCache = new();
  private static readonly ConcurrentDictionary<(Type HandlerType, Type ResponseType), object> HandlerDelegateCache = new();
  private static readonly ConcurrentDictionary<(Type DecoratorType, Type ResponseType), object> DecoratorDelegateCache = new();

  /// <summary>
  /// Processes a request and returns the response wrapped in a Result.
  /// </summary>
  /// <typeparam name="TResponse">The type of the response.</typeparam>
  /// <param name="request">The request to process.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A task that represents the async operation, containing the result of the request.</returns>
  /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
  /// <remarks>
  /// <para>
  /// Execution flow:
  /// <list type="number">
  ///   <item><description>Validates request parameter and logs processing start</description></item>
  ///   <item><description>Resolves handler type using cached type mapping for performance</description></item>
  ///   <item><description>Attempts to resolve handler instance from DI container</description></item>
  ///   <item><description>Discovers and resolves decorators for request/response type combination</description></item>
  ///   <item><description>Builds decorator pipeline or executes handler directly based on decorator availability</description></item>
  ///   <item><description>Executes request through pipeline using compiled delegates for optimal performance</description></item>
  ///   <item><description>Handles exceptions and converts them to appropriate Result failures</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Error handling strategy:
  /// <list type="bullet">
  ///   <item><description>Handler not found: Returns Result.Failure with NotFound error type</description></item>
  ///   <item><description>OperationCanceledException: Converts to ServiceUnavailable with cancellation context</description></item>
  ///   <item><description>All other exceptions: Wrapped in ServiceUnavailable error with full exception details</description></item>
  ///   <item><description>Business failures: Propagated through Result pattern without exception handling</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  [SuppressMessage(
    "Minor Code Smell",
    "S2221:\"Exception\" should not be caught",
    Justification = "Mediator pattern requires catching all exceptions to convert them to Result failures. This is the boundary between exception-based and Result-based error handling.")]
  public async Task<Result<TResponse>> ProcessAsync<TResponse>(
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default) {

    ArgumentNullException.ThrowIfNull(request);

    var requestType = request.GetType();
    var responseType = typeof(TResponse);
    var requestName = requestType.Name;

    _Logger.LogMediatorProcessingRequest(requestName);

    try {
      var handlerType = GetOrCreateHandlerType(requestType, responseType);
      var handler = _ServiceProvider.GetService(handlerType);

      if (handler is null) {
        _Logger.LogMediatorHandlerNotFound(requestName);
        return Result.Failure<TResponse>(ResultError.NotFound(
          "HANDLER_NOT_FOUND",
          $"No handler found for request type '{requestName}'"));
      }

      // Get decorators for this request/response type combination  
      var decorators = GetDecorators(requestType, responseType);

      // Execute through pipeline if decorators exist, otherwise execute handler directly
      if (decorators.Any()) {
        var pipeline = BuildPipeline(handler, decorators, request, requestType, responseType, cancellationToken);
        var result = await pipeline();

        _Logger.LogMediatorProcessedRequest(requestName, result.IsSuccess);
        return result;
      } else {
        // Execute handler directly using compiled delegate
        _Logger.LogMediatorExecutingHandler(handlerType, requestName);

        var handlerDelegate = GetOrCreateHandlerDelegate<TResponse>(handlerType);
        var result = await handlerDelegate(handler, request, cancellationToken);

        _Logger.LogMediatorProcessedRequest(requestName, result.IsSuccess);
        return result;
      }
    } catch (OperationCanceledException) {
      _Logger.LogMediatorRequestCancelled(requestName);
      return Result.Failure<TResponse>(ResultError.ServiceUnavailable(
        "REQUEST_CANCELLED",
        "The request was cancelled"));
    } catch (Exception ex) {
      _Logger.LogMediatorRequestProcessError(requestName, ex);
      return Result.Failure<TResponse>(ResultError.ServiceUnavailable(
        "MEDIATOR_PROCESSING_ERROR",
        "An error occurred while processing the request",
        ex));
    }
  }

  /// <summary>
  /// Dispatches a notification to all registered handlers.
  /// </summary>
  /// <typeparam name="TNotification">The concrete type of the notification.</typeparam>
  /// <param name="notification">The notification to dispatch.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A task that represents the async operation.</returns>
  /// <exception cref="ArgumentNullException">Thrown when notification is null.</exception>
  /// <remarks>
  /// <para>
  /// Fire-and-forget execution model:
  /// <list type="bullet">
  ///   <item><description>Parallel execution: All handlers execute concurrently for maximum throughput</description></item>
  ///   <item><description>Error isolation: Failure of one handler does not affect others</description></item>
  ///   <item><description>Non-blocking: Method returns when all handlers are started, not completed</description></item>
  ///   <item><description>Exception safety: All exceptions are caught, logged, and isolated</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The method uses Task.WhenAll to wait for all handler tasks to complete, ensuring proper
  /// exception handling and logging while maintaining the fire-and-forget semantics for the caller.
  /// </para>
  /// </remarks>
  [SuppressMessage(
    "Minor Code Smell",
    "S2221:\"Exception\" should not be caught",
    Justification = "Notification dispatching uses fire-and-forget semantics where exceptions must be caught and logged but not propagated to maintain system stability.")]
  public async Task DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
    where TNotification : INotification {

    ArgumentNullException.ThrowIfNull(notification);

    var notificationName = typeof(TNotification).Name;

    _Logger.LogMediatorNotificationProcessing(notificationName);

    try {
      var handlers = _ServiceProvider.GetServices<IRequestHandler<TNotification, Unit>>();
      var handlersList = handlers.ToList();

      if (handlersList.Count > 0) {
        _Logger.LogMediatorNotificationDispatchingTo(notificationName, handlersList.Count);

        // Execute all handlers in parallel but wait for completion for proper error handling
        var tasks = handlersList.Select(handler =>
          ExecuteNotificationHandlerAsync(handler, notification, handler.GetType().Name, notificationName, cancellationToken));

        await Task.WhenAll(tasks);

        _Logger.LogMediatorNotificationProcessed(notificationName);
      } else {
        _Logger.LogMediatorNotificationNoHandler(notificationName);
      }
    } catch (Exception ex) {
      // Log the exception but don't throw - notifications should be fire-and-forget
      _Logger.LogMediatorNotificationProcessError(notificationName, ex);
    }
  }

  /// <summary>
  /// Gets the decorators for a specific request and response type combination.
  /// </summary>
  /// <param name="requestType">The concrete request type.</param>
  /// <param name="responseType">The response type.</param>
  /// <returns>An enumerable of decorators for the request/response combination.</returns>
  /// <remarks>
  /// <para>
  /// This method uses graceful degradation: if decorator resolution fails for any reason,
  /// it returns an empty collection and logs the error, allowing the request to proceed
  /// without decorators rather than failing completely.
  /// </para>
  /// </remarks>
  [SuppressMessage(
    "Minor Code Smell",
    "S2221:\"Exception\" should not be caught",
    Justification = "Decorator resolution uses graceful degradation where exceptions are caught and logged, but the request continues without decorators to maintain system availability.")]
  private IEnumerable<object> GetDecorators(Type requestType, Type responseType) {
    try {
      var decoratorType = GetOrCreateDecoratorType(requestType, responseType);
      var decorators = _ServiceProvider.GetServices(decoratorType);
      return decorators.Cast<object>();
    } catch (Exception ex) {
      _Logger.LogPipelineDecoratorResolutionError(requestType.Name, ex);
      return [];
    }
  }

  /// <summary>
  /// Builds a pipeline of decorators around the final handler using compiled delegates.
  /// </summary>
  /// <typeparam name="TResponse">The response type.</typeparam>
  /// <param name="finalHandler">The final handler instance.</param>
  /// <param name="decorators">The decorator instances.</param>
  /// <param name="request">The request to process.</param>
  /// <param name="requestType">The concrete request type.</param>
  /// <param name="responseType">The response type.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A function that executes the pipeline.</returns>
  /// <remarks>
  /// <para>
  /// Pipeline construction strategy:
  /// <list type="bullet">
  ///   <item><description>Inner-to-outer wrapping: Decorators wrap the handler in reverse order</description></item>
  ///   <item><description>Compiled delegates: Each decorator and handler uses pre-compiled expressions</description></item>
  ///   <item><description>Closure capture: Pipeline captures request and cancellation token efficiently</description></item>
  ///   <item><description>Type safety: All type conversions are validated at compile time</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  private static RequestHandlerFunction<TResponse> BuildPipeline<TResponse>(
    object finalHandler,
    IEnumerable<object> decorators,
    IRequest<TResponse> request,
    Type requestType,
    Type responseType,
    CancellationToken cancellationToken) {

    // Create the final handler function using compiled delegate
    var handlerType = GetOrCreateHandlerType(requestType, responseType);
    var handlerDelegate = GetOrCreateHandlerDelegate<TResponse>(handlerType);

    RequestHandlerFunction<TResponse> pipeline = () =>
      handlerDelegate(finalHandler, request, cancellationToken);

    // Wrap each decorator around the pipeline, in reverse order
    var decoratorList = decorators.Reverse().ToList();
    var decoratorType = GetOrCreateDecoratorType(requestType, responseType);
    var decoratorDelegate = GetOrCreateDecoratorDelegate<TResponse>(decoratorType);

    foreach (var decorator in decoratorList) {
      var currentPipeline = pipeline;
      pipeline = () => decoratorDelegate(decorator, request, currentPipeline, cancellationToken);
    }

    return pipeline;
  }

  /// <summary>
  /// Executes a single notification handler with error isolation.
  /// </summary>
  /// <typeparam name="TNotification">The concrete type of the notification.</typeparam>
  /// <param name="handler">The handler instance.</param>
  /// <param name="notification">The notification to handle.</param>
  /// <param name="handlerTypeName">The handler type name for logging.</param>
  /// <param name="notificationName">The notification name for logging.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A task representing the handler execution.</returns>
  /// <remarks>
  /// <para>
  /// Error isolation ensures that exceptions in one handler do not affect the execution
  /// of other handlers, maintaining the reliability of the notification system.
  /// </para>
  /// </remarks>
  [SuppressMessage(
    "Minor Code Smell",
    "S2221:\"Exception\" should not be caught",
    Justification = "Notification handler isolation requires catching all exceptions to prevent one failing handler from affecting others in the parallel execution model.")]
  private async Task ExecuteNotificationHandlerAsync<TNotification>(
    IRequestHandler<TNotification, Unit> handler,
    TNotification notification,
    string handlerTypeName,
    string notificationName,
    CancellationToken cancellationToken)
    where TNotification : INotification {

    try {
      var result = await handler.HandleAsync(notification, cancellationToken);

      if (result.IsFailure) {
        _Logger.LogMediatorNotificationHandlerFailed(
          handlerTypeName,
          notificationName,
          result.Error);
      } else {
        _Logger.LogMediatorNotificationHandlerSucceed(
          handlerTypeName,
          notificationName);
      }
    } catch (Exception ex) {
      // Isolate handler failures - one failing handler shouldn't affect others
      _Logger.LogMediatorNotificationHandlerError(
        handlerTypeName,
        notificationName,
        ex);
    }
  }

  /// <summary>
  /// Gets or creates a cached handler type for the given request and response types.
  /// </summary>
  /// <param name="requestType">The request type.</param>
  /// <param name="responseType">The response type.</param>
  /// <returns>The handler type interface.</returns>
  private static Type GetOrCreateHandlerType(Type requestType, Type responseType) =>
    HandlerTypeCache.GetOrAdd((requestType, responseType),
      key => typeof(IRequestHandler<,>).MakeGenericType(key.RequestType, key.ResponseType));

  /// <summary>
  /// Gets or creates a cached decorator type for the given request and response types.
  /// </summary>
  /// <param name="requestType">The request type.</param>
  /// <param name="responseType">The response type.</param>
  /// <returns>The decorator type interface.</returns>
  private static Type GetOrCreateDecoratorType(Type requestType, Type responseType) =>
    DecoratorTypeCache.GetOrAdd((requestType, responseType),
      key => typeof(IRequestDecorator<,>).MakeGenericType(key.RequestType, key.ResponseType));

  /// <summary>
  /// Gets or creates a compiled delegate for handler execution with proper typing.
  /// </summary>
  /// <typeparam name="TResponse">The response type.</typeparam>
  /// <param name="handlerType">The handler interface type.</param>
  /// <returns>A compiled delegate for fast handler execution.</returns>
  /// <remarks>
  /// <para>
  /// Performance optimization: This method creates and caches compiled expressions that
  /// eliminate reflection overhead during handler execution. The compiled delegates
  /// are significantly faster than reflection-based invocation.
  /// </para>
  /// </remarks>
  private static Func<object, object, CancellationToken, Task<Result<TResponse>>> GetOrCreateHandlerDelegate<TResponse>(Type handlerType) {
    var key = (handlerType, typeof(TResponse));

    var cachedDelegate = HandlerDelegateCache.GetOrAdd(key, _ => {
      // Get the generic arguments to reconstruct TRequest
      var genericArgs = handlerType.GetGenericArguments();
      var requestType = genericArgs[0];

      // Create expression: (handler, request, ct) => ((IRequestHandler<TRequest, TResponse>)handler).HandleAsync((TRequest)request, ct)
      var handlerParam = Expression.Parameter(typeof(object), "handler");
      var requestParam = Expression.Parameter(typeof(object), "request");
      var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

      var typedHandler = Expression.Convert(handlerParam, handlerType);
      var typedRequest = Expression.Convert(requestParam, requestType);

      var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!;
      var methodCall = Expression.Call(typedHandler, handleMethod, typedRequest, cancellationTokenParam);

      var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<Result<TResponse>>>>(
        methodCall, handlerParam, requestParam, cancellationTokenParam);

      return lambda.Compile();
    });

    return (Func<object, object, CancellationToken, Task<Result<TResponse>>>)cachedDelegate;
  }

  /// <summary>
  /// Gets or creates a compiled delegate for decorator execution with proper typing.
  /// </summary>
  /// <typeparam name="TResponse">The response type.</typeparam>
  /// <param name="decoratorType">The decorator interface type.</param>
  /// <returns>A compiled delegate for fast decorator execution.</returns>
  /// <remarks>
  /// <para>
  /// Performance optimization: Similar to handler delegates, this creates cached compiled
  /// expressions for decorator execution, eliminating reflection overhead in the decorator pipeline.
  /// </para>
  /// </remarks>
  private static Func<object, object, RequestHandlerFunction<TResponse>, CancellationToken, Task<Result<TResponse>>> GetOrCreateDecoratorDelegate<TResponse>(Type decoratorType) {
    var key = (decoratorType, typeof(TResponse));

    var cachedDelegate = DecoratorDelegateCache.GetOrAdd(key, _ => {
      // Get the generic arguments to reconstruct TRequest
      var genericArgs = decoratorType.GetGenericArguments();
      var requestType = genericArgs[0];

      // Create expression: (decorator, request, continuation, ct) => ((IRequestDecorator<TRequest, TResponse>)decorator).HandleAsync((TRequest)request, continuation, ct)
      var decoratorParam = Expression.Parameter(typeof(object), "decorator");
      var requestParam = Expression.Parameter(typeof(object), "request");
      var continuationParam = Expression.Parameter(typeof(RequestHandlerFunction<TResponse>), "continuation");
      var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

      var typedDecorator = Expression.Convert(decoratorParam, decoratorType);
      var typedRequest = Expression.Convert(requestParam, requestType);

      var handleMethod = decoratorType.GetMethod(nameof(IRequestDecorator<IRequest<TResponse>, TResponse>.HandleAsync))!;
      var methodCall = Expression.Call(typedDecorator, handleMethod, typedRequest, continuationParam, cancellationTokenParam);

      var lambda = Expression.Lambda<Func<object, object, RequestHandlerFunction<TResponse>, CancellationToken, Task<Result<TResponse>>>>(
        methodCall, decoratorParam, requestParam, continuationParam, cancellationTokenParam);

      return lambda.Compile();
    });

    return (Func<object, object, RequestHandlerFunction<TResponse>, CancellationToken, Task<Result<TResponse>>>)cachedDelegate;
  }
}
