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
/// Key features:
/// <list type="bullet">
///   <item><description>Automatic handler resolution via dependency injection</description></item>
///   <item><description>Type-safe request/response handling</description></item>
///   <item><description>Fire-and-forget notification dispatching</description></item>
///   <item><description>Decorator pipeline for cross-cutting concerns</description></item>
///   <item><description>High-performance compiled delegates</description></item>
///   <item><description>Comprehensive error handling with Result pattern</description></item>
/// </list>
/// </para>
/// <para>
/// The mediator is thread-safe and can be registered as a singleton or scoped service
/// in the dependency injection container.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="MediatorImplementation"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider for resolving handlers.</param>
/// <param name="logger">The logger for diagnostic information.</param>
/// <exception cref="ArgumentNullException">
/// Thrown when serviceProvider or logger is null.
/// </exception>
public class MediatorImplementation(IServiceProvider serviceProvider, ILogger<MediatorImplementation> logger): IMediator {
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
  [SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught", Justification = "We want catch anything.")]
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
  [SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught", Justification = "We want catch anything.")]
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

        // Execute all handlers in parallel but don't wait for completion
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
  [SuppressMessage(
    "Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "Use direct LogDebug here as this is an exceptional case not covered by LoggerMessage extensions")]
  [SuppressMessage(
    "Minor Code Smell",
    "S2221:\"Exception\" should not be caught",
    Justification = "We want to catch all exceptions during decorator resolution to gracefully continue without decorators.")]
  private IEnumerable<object> GetDecorators(
    Type requestType,
    Type responseType) {

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
  [SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught", Justification = "We want catch anything.")]
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
