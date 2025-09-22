using Microsoft.Extensions.Logging;

using NSI.Core.Results;

namespace NSI.Core.Mediator;
/// <summary>
/// High-performance logging extensions using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// This class provides compiled logging methods that avoid boxing and string allocation
/// overhead compared to traditional ILogger extension calls. All methods use source
/// generated LoggerMessage delegates for optimal performance.
/// </para>
/// <para>
/// Event ID organization:
/// <list type="bullet">
///   <item><description>1-19: Core Mediator Processing</description></item>
///   <item><description>20-29: Logging Decorator</description></item>
///   <item><description>30-39: Validation Decorator</description></item>
///   <item><description>40-49: Validation System</description></item>
///   <item><description>50-59: Health Checks</description></item>
///   <item><description>60-69: Pipeline System</description></item>
///   <item><description>70-79: Performance and Metrics</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class ILoggerExtensions {

  #region Core Mediator Processing (EventId 1-19)

  [LoggerMessage(
      EventId = 1,
      EventName = "MediatorProcessingRequest",
      Level = LogLevel.Debug,
      Message = "Processing request: {RequestType}"
  )]
  public static partial void LogMediatorProcessingRequest(this ILogger logger, string? requestType);

  [LoggerMessage(
      EventId = 2,
      EventName = "MediatorHandlerNotFound",
      Level = LogLevel.Debug,
      Message = "Handler not found for request: {RequestName}"
  )]
  public static partial void LogMediatorHandlerNotFound(this ILogger logger, string? requestName);

  [LoggerMessage(
    EventId = 3,
    EventName = "MediatorHandleMethodNotFound",
    Level = LogLevel.Debug,
    Message = "HandleAsync method not found on handler: {HandlerType}"
  )]
  public static partial void LogMediatorHandleMethodNotFound(this ILogger logger, Type handlerType);

  [LoggerMessage(
    EventId = 4,
    EventName = "MediatorExecutingHandler",
    Level = LogLevel.Debug,
    Message = "Executing handler: {HandlerType} for request: {RequestName}"
  )]
  public static partial void LogMediatorExecutingHandler(this ILogger logger, Type handlerType, string? requestName);

  [LoggerMessage(
    EventId = 5,
    EventName = "MediatorProcessedRequest",
    Level = LogLevel.Debug,
    Message = "Request {RequestName} processed successfully: {IsSuccess}"
  )]
  public static partial void LogMediatorProcessedRequest(this ILogger logger, string? requestName, bool isSuccess);

  [LoggerMessage(
    EventId = 6,
    EventName = "MediatorRequestCancelled",
    Level = LogLevel.Information,
    Message = "Request {RequestName} was cancelled"
  )]
  public static partial void LogMediatorRequestCancelled(this ILogger logger, string? requestName);

  [LoggerMessage(
    EventId = 7,
    EventName = "MediatorRequestProcessError",
    Level = LogLevel.Error,
    Message = "Error processing request: {RequestName}"
  )]
  public static partial void LogMediatorRequestProcessError(this ILogger logger, string? requestName, Exception ex);

  [LoggerMessage(
    EventId = 8,
    EventName = "MediatorNotificationProcessing",
    Level = LogLevel.Debug,
    Message = "Dispatching notification: {NotificationName}"
  )]
  public static partial void LogMediatorNotificationProcessing(this ILogger logger, string? notificationName);

  [LoggerMessage(
    EventId = 9,
    EventName = "MediatorNotificationDispatchingTo",
    Level = LogLevel.Debug,
    Message = "Dispatching notification {NotificationName} to {HandlerCount} handlers"
  )]
  public static partial void LogMediatorNotificationDispatchingTo(this ILogger logger, string? notificationName, int handlerCount);

  [LoggerMessage(
    EventId = 10,
    EventName = "MediatorNotificationProcessed",
    Level = LogLevel.Debug,
    Message = "All handlers for notification {NotificationName} have been executed"
  )]
  public static partial void LogMediatorNotificationProcessed(this ILogger logger, string? notificationName);

  [LoggerMessage(
    EventId = 11,
    EventName = "MediatorNotificationNoHandler",
    Level = LogLevel.Debug,
    Message = "No handlers found for notification: {NotificationName}"
  )]
  public static partial void LogMediatorNotificationNoHandler(this ILogger logger, string? notificationName);

  [LoggerMessage(
    EventId = 12,
    EventName = "MediatorNotificationProcessError",
    Level = LogLevel.Error,
    Message = "Error dispatching notification: {NotificationName}"
  )]
  public static partial void LogMediatorNotificationProcessError(this ILogger logger, string? notificationName, Exception ex);

  [LoggerMessage(
    EventId = 13,
    EventName = "MediatorNotificationHandlerFailed",
    Level = LogLevel.Warning,
    Message = "Notification handler {HandlerType} failed for notification {NotificationName}: {Error}"
  )]
  public static partial void LogMediatorNotificationHandlerFailed(this ILogger logger, string? handlerType, string? notificationName, ResultError error);

  [LoggerMessage(
    EventId = 14,
    EventName = "MediatorNotificationHandlerSucceed",
    Level = LogLevel.Debug,
    Message = "Notification handler {HandlerType} completed successfully for notification {NotificationName}"
  )]
  public static partial void LogMediatorNotificationHandlerSucceed(this ILogger logger, string? handlerType, string? notificationName);

  [LoggerMessage(
    EventId = 15,
    EventName = "MediatorNotificationHandlerError",
    Level = LogLevel.Error,
    Message = "Exception in notification handler {HandlerType} for notification {NotificationName}"
  )]
  public static partial void LogMediatorNotificationHandlerError(this ILogger logger, string? handlerType, string? notificationName, Exception ex);

  #endregion

  #region Logging Decorator (EventId 20-29)

  [LoggerMessage(
    EventId = 20,
    EventName = "DecoratorRequestStarting",
    Level = LogLevel.Information,
    Message = "Starting request processing: {RequestName} (CorrelationId: {CorrelationId})"
  )]
  public static partial void LogDecoratorRequestStarting(this ILogger logger, string requestName, string correlationId);

  [LoggerMessage(
    EventId = 21,
    EventName = "DecoratorRequestCompleted",
    Level = LogLevel.Information,
    Message = "Request {RequestName} completed successfully in {ElapsedMs}ms"
  )]
  public static partial void LogDecoratorRequestCompleted(this ILogger logger, string requestName, long elapsedMs);

  [LoggerMessage(
    EventId = 22,
    EventName = "DecoratorRequestFailed",
    Level = LogLevel.Warning,
    Message = "Request {RequestName} failed in {ElapsedMs}ms with error: {ErrorType} - {ErrorCode}: {ErrorMessage}"
  )]
  public static partial void LogDecoratorRequestFailed(this ILogger logger, string requestName, long elapsedMs, ErrorType errorType, string errorCode, string errorMessage);

  [LoggerMessage(
    EventId = 23,
    EventName = "DecoratorRequestCancelled",
    Level = LogLevel.Warning,
    Message = "Request {RequestName} was cancelled after {ElapsedMs}ms"
  )]
  public static partial void LogDecoratorRequestCancelled(this ILogger logger, string requestName, long elapsedMs);

  [LoggerMessage(
    EventId = 24,
    EventName = "DecoratorRequestException",
    Level = LogLevel.Error,
    Message = "Request {RequestName} threw an exception after {ElapsedMs}ms"
  )]
  public static partial void LogDecoratorRequestException(this ILogger logger, string requestName, long elapsedMs, Exception ex);

  #endregion

  #region Validation Decorator (EventId 30-39)

  [LoggerMessage(
    EventId = 30,
    EventName = "DecoratorValidationStarting",
    Level = LogLevel.Debug,
    Message = "Validating request: {RequestName}"
  )]
  public static partial void LogDecoratorValidationStarting(this ILogger logger, string requestName);

  [LoggerMessage(
    EventId = 31,
    EventName = "DecoratorValidationPassed",
    Level = LogLevel.Debug,
    Message = "Request {RequestName} validation passed"
  )]
  public static partial void LogDecoratorValidationPassed(this ILogger logger, string requestName);

  [LoggerMessage(
    EventId = 32,
    EventName = "DecoratorValidationFailed",
    Level = LogLevel.Warning,
    Message = "Validation failed for {RequestName}: {ValidationErrors}"
  )]
  public static partial void LogDecoratorValidationFailed(this ILogger logger, string requestName, string validationErrors);

  [LoggerMessage(
    EventId = 33,
    EventName = "DecoratorValidationSkipped",
    Level = LogLevel.Debug,
    Message = "Validation skipped for {RequestName}: No validator registered"
  )]
  public static partial void LogDecoratorValidationSkipped(this ILogger logger, string requestName);

  [LoggerMessage(
    EventId = 34,
    EventName = "DecoratorValidationDataAnnotations",
    Level = LogLevel.Debug,
    Message = "Performing Data Annotations validation for {RequestName}"
  )]
  public static partial void LogDecoratorValidationDataAnnotations(this ILogger logger, string requestName);

  [LoggerMessage(
    EventId = 35,
    EventName = "DecoratorValidationCustom",
    Level = LogLevel.Debug,
    Message = "Performing custom validation for {RequestName}"
  )]
  public static partial void LogDecoratorValidationCustom(this ILogger logger, string requestName);

  [LoggerMessage(
    EventId = 36,
    EventName = "DecoratorValidationCustomFailed",
    Level = LogLevel.Warning,
    Message = "Custom validation failed for {RequestName}: {ErrorMessage}"
  )]
  public static partial void LogDecoratorValidationCustomFailed(this ILogger logger, string requestName, string errorMessage);

  #endregion

  #region Validation System (EventId 40-49)

  [LoggerMessage(
    EventId = 40,
    EventName = "MediatorValidationNoHandler",
    Level = LogLevel.Error,
    Message = "Handler validation failed: {Error}"
  )]
  public static partial void LogMediatorValidationNoHandler(this ILogger logger, string? error);

  [LoggerMessage(
    EventId = 41,
    EventName = "MediatorValidationSuccess",
    Level = LogLevel.Debug,
    Message = "Handler validation passed: {RequestName} -> {HandlerType}"
  )]
  public static partial void LogMediatorValidationSuccess(this ILogger logger, string? requestName, Type handlerType);

  [LoggerMessage(
    EventId = 42,
    EventName = "MediatorValidationFailure",
    Level = LogLevel.Error,
    Message = "Mediator handler validation failed. {ErrorCount} missing handlers found."
  )]
  public static partial void LogMediatorValidationFailure(this ILogger logger, int errorCount);

  [LoggerMessage(
    EventId = 43,
    EventName = "MediatorValidationGlobalSuccess",
    Level = LogLevel.Information,
    Message = "Mediator handler validation completed successfully. {RequestCount} request types validated."
  )]
  public static partial void LogMediatorValidationGlobalSuccess(this ILogger logger, int requestCount);

  [LoggerMessage(
    EventId = 44,
    EventName = "MediatorValidationStarting",
    Level = LogLevel.Information,
    Message = "Starting mediator handler validation for {AssemblyCount} assemblies"
  )]
  public static partial void LogMediatorValidationStarting(this ILogger logger, int assemblyCount);

  [LoggerMessage(
    EventId = 45,
    EventName = "MediatorValidationAssemblyScanning",
    Level = LogLevel.Debug,
    Message = "Scanning assembly {AssemblyName} for request types"
  )]
  public static partial void LogMediatorValidationAssemblyScanning(this ILogger logger, string assemblyName);

  #endregion

  #region Health Checks (EventId 50-59)

  [LoggerMessage(
    EventId = 50,
    EventName = "MediatorHealthCheckStarting",
    Level = LogLevel.Debug,
    Message = "Starting mediator health check"
  )]
  public static partial void LogMediatorHealthCheckStarting(this ILogger logger);

  [LoggerMessage(
    EventId = 51,
    EventName = "MediatorHealthCheckCompleted",
    Level = LogLevel.Debug,
    Message = "Mediator health check completed successfully"
  )]
  public static partial void LogMediatorHealthCheckCompleted(this ILogger logger);

  [LoggerMessage(
    EventId = 52,
    EventName = "MediatorHealthCheckFailed",
    Level = LogLevel.Warning,
    Message = "Mediator health check failed: {Error}"
  )]
  public static partial void LogMediatorHealthCheckFailed(this ILogger logger, ResultError error);

  [LoggerMessage(
    EventId = 53,
    EventName = "MediatorHealthCheckError",
    Level = LogLevel.Error,
    Message = "Mediator health check threw an exception"
  )]
  public static partial void LogMediatorHealthCheckError(this ILogger logger, Exception ex);

  [LoggerMessage(
    EventId = 54,
    EventName = "MediatorHealthCheckTimeout",
    Level = LogLevel.Warning,
    Message = "Mediator health check timed out after {TimeoutMs}ms"
  )]
  public static partial void LogMediatorHealthCheckTimeout(this ILogger logger, long timeoutMs);

  [LoggerMessage(
    EventId = 55,
    EventName = "MediatorHealthCheckStarted",
    Level = LogLevel.Debug,
    Message = "Mediator health check started successfully"
  )]
  public static partial void LogMediatorHealthCheckStarted(this ILogger logger);

  #endregion

  #region Pipeline System (EventId 60-69)

  [LoggerMessage(
    EventId = 60,
    EventName = "PipelineDecoratorResolutionFailed",
    Level = LogLevel.Debug,
    Message = "Failed to resolve decorators for {RequestType}, continuing without decorators"
  )]
  public static partial void LogPipelineDecoratorResolutionFailed(this ILogger logger, string requestType, Exception ex);

  [LoggerMessage(
    EventId = 61,
    EventName = "PipelineExecuting",
    Level = LogLevel.Debug,
    Message = "Executing pipeline for {RequestType} with {DecoratorCount} decorators"
  )]
  public static partial void LogPipelineExecuting(this ILogger logger, string requestType, int decoratorCount);

  [LoggerMessage(
    EventId = 62,
    EventName = "PipelineCompleted",
    Level = LogLevel.Debug,
    Message = "Pipeline execution completed for {RequestType} in {ElapsedMs}ms"
  )]
  public static partial void LogPipelineCompleted(this ILogger logger, string requestType, long elapsedMs);

  [LoggerMessage(
    EventId = 63,
    EventName = "PipelineDecoratorRegistered",
    Level = LogLevel.Debug,
    Message = "Registered decorator {DecoratorType} for request/response pattern"
  )]
  public static partial void LogPipelineDecoratorRegistered(this ILogger logger, Type decoratorType);

  [LoggerMessage(
    EventId = 64,
    EventName = "PipelineBuilderCreating",
    Level = LogLevel.Debug,
    Message = "Building pipeline with {DecoratorCount} decorators for {RequestType} -> {ResponseType}"
  )]
  public static partial void LogPipelineBuilderCreating(this ILogger logger, int decoratorCount, string requestType, string responseType);

  [LoggerMessage(
    EventId = 65,
    EventName = "PipelineDecoratorExecuting",
    Level = LogLevel.Trace,
    Message = "Executing decorator {DecoratorType} for request {RequestName}"
  )]
  public static partial void LogPipelineDecoratorExecuting(this ILogger logger, string decoratorType, string requestName);

  [LoggerMessage(
    EventId = 66,
    EventName = "PipelineDecoratorCompleted",
    Level = LogLevel.Trace,
    Message = "Completed decorator {DecoratorType} for request {RequestName} in {ElapsedMs}ms"
  )]
  public static partial void LogPipelineDecoratorCompleted(this ILogger logger, string decoratorType, string requestName, long elapsedMs);

  [LoggerMessage(
    EventId = 67,
    EventName = "PipelineDecoratorResolutionError",
    Level = LogLevel.Debug,
    Message = "Failed to resolve decorators for {RequestName}, continuing without decorators"
  )]
  public static partial void LogPipelineDecoratorResolutionError(this ILogger logger, string requestName, Exception ex);

  #endregion

  #region Performance and Metrics (EventId 70-79)

  [LoggerMessage(
    EventId = 70,
    EventName = "PerformanceCacheHit",
    Level = LogLevel.Trace,
    Message = "Cache hit for {CacheType}: {CacheKey}"
  )]
  public static partial void LogPerformanceCacheHit(this ILogger logger, string cacheType, string cacheKey);

  [LoggerMessage(
    EventId = 71,
    EventName = "PerformanceCacheMiss",
    Level = LogLevel.Trace,
    Message = "Cache miss for {CacheType}: {CacheKey}"
  )]
  public static partial void LogPerformanceCacheMiss(this ILogger logger, string cacheType, string cacheKey);

  [LoggerMessage(
    EventId = 72,
    EventName = "PerformanceSlowRequest",
    Level = LogLevel.Information,
    Message = "Slow request detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)"
  )]
  public static partial void LogPerformanceSlowRequest(this ILogger logger, string requestName, long elapsedMs, long thresholdMs);

  [LoggerMessage(
    EventId = 73,
    EventName = "PerformanceMemoryUsage",
    Level = LogLevel.Debug,
    Message = "Memory usage after processing {RequestName}: {MemoryMB}MB (Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2})"
  )]
  public static partial void LogPerformanceMemoryUsage(this ILogger logger, string requestName, long memoryMB, int gen0, int gen1, int gen2);

  [LoggerMessage(
    EventId = 74,
    EventName = "PerformanceHighConcurrency",
    Level = LogLevel.Information,
    Message = "High concurrency detected: {ActiveRequests} active requests for {RequestType}"
  )]
  public static partial void LogPerformanceHighConcurrency(this ILogger logger, int activeRequests, string requestType);

  [LoggerMessage(
    EventId = 75,
    EventName = "PerformanceExpressionCompiled",
    Level = LogLevel.Debug,
    Message = "Compiled expression for {HandlerType} in {CompilationTimeMs}ms"
  )]
  public static partial void LogPerformanceExpressionCompiled(this ILogger logger, Type handlerType, long compilationTimeMs);

  [LoggerMessage(
    EventId = 76,
    EventName = "PerformanceReflectionFallback",
    Level = LogLevel.Debug,
    Message = "Falling back to reflection for {HandlerType} due to compilation failure"
  )]
  public static partial void LogPerformanceReflectionFallback(this ILogger logger, Type handlerType);

  #endregion

  #region Service Registration and Discovery (EventId 80-89)

  [LoggerMessage(
    EventId = 80,
    EventName = "ServiceRegistrationStarting",
    Level = LogLevel.Information,
    Message = "Starting service registration scan for {AssemblyCount} assemblies"
  )]
  public static partial void LogServiceRegistrationStarting(this ILogger logger, int assemblyCount);

  [LoggerMessage(
    EventId = 81,
    EventName = "ServiceRegistrationHandlerFound",
    Level = LogLevel.Debug,
    Message = "Found handler {HandlerType} implementing {InterfaceType}"
  )]
  public static partial void LogServiceRegistrationHandlerFound(this ILogger logger, Type handlerType, Type interfaceType);

  [LoggerMessage(
    EventId = 82,
    EventName = "ServiceRegistrationDecoratorFound",
    Level = LogLevel.Debug,
    Message = "Found decorator {DecoratorType} implementing {InterfaceType}"
  )]
  public static partial void LogServiceRegistrationDecoratorFound(this ILogger logger, Type decoratorType, Type interfaceType);

  [LoggerMessage(
    EventId = 83,
    EventName = "ServiceRegistrationCompleted",
    Level = LogLevel.Information,
    Message = "Service registration completed: {HandlerCount} handlers, {DecoratorCount} decorators registered"
  )]
  public static partial void LogServiceRegistrationCompleted(this ILogger logger, int handlerCount, int decoratorCount);

  [LoggerMessage(
    EventId = 84,
    EventName = "ServiceRegistrationSkipped",
    Level = LogLevel.Debug,
    Message = "Skipped registration for {TypeName}: {Reason}"
  )]
  public static partial void LogServiceRegistrationSkipped(this ILogger logger, string typeName, string reason);

  #endregion

  #region Configuration and Startup (EventId 90-99)

  [LoggerMessage(
    EventId = 90,
    EventName = "ConfigurationLoaded",
    Level = LogLevel.Information,
    Message = "Mediator configuration loaded: Timeout={TimeoutMs}ms, DetailedLogging={DetailedLogging}, MaxConcurrentNotifications={MaxConcurrent}"
  )]
  public static partial void LogConfigurationLoaded(this ILogger logger, long timeoutMs, bool detailedLogging, int maxConcurrent);

  [LoggerMessage(
    EventId = 91,
    EventName = "ConfigurationInvalid",
    Level = LogLevel.Error,
    Message = "Invalid mediator configuration: {ValidationError}"
  )]
  public static partial void LogConfigurationInvalid(this ILogger logger, string validationError);

  [LoggerMessage(
    EventId = 92,
    EventName = "StartupInitialization",
    Level = LogLevel.Information,
    Message = "Initializing mediator with {HandlerCount} handlers and {DecoratorCount} decorators"
  )]
  public static partial void LogStartupInitialization(this ILogger logger, int handlerCount, int decoratorCount);

  [LoggerMessage(
    EventId = 93,
    EventName = "StartupComplete",
    Level = LogLevel.Information,
    Message = "Mediator initialization completed successfully in {InitializationTimeMs}ms"
  )]
  public static partial void LogStartupComplete(this ILogger logger, long initializationTimeMs);

  [LoggerMessage(
    EventId = 94,
    EventName = "StartupWarning",
    Level = LogLevel.Warning,
    Message = "Mediator startup warning: {WarningMessage}"
  )]
  public static partial void LogStartupWarning(this ILogger logger, string warningMessage);

  #endregion
}
