using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator.HealthChecks;

/// <summary>
/// Health check implementation for validating mediator service functionality.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that the mediator infrastructure is functioning correctly by
/// executing a lightweight test query through the complete mediator pipeline. It validates
/// that all core components including handlers, decorators, and service dependencies are
/// properly configured and operational.
/// </para>
/// <para>
/// Validation performed:
/// <list type="bullet">
///   <item><description>Handler registration and resolution from DI container</description></item>
///   <item><description>Decorator pipeline execution (logging, validation, etc.)</description></item>
///   <item><description>Request processing through complete mediator workflow</description></item>
///   <item><description>Service provider configuration and dependency availability</description></item>
/// </list>
/// </para>
/// <para>
/// Health status determination:
/// <list type="bullet">
///   <item><description><see cref="HealthStatus.Healthy"/>: Mediator processes test query successfully</description></item>
///   <item><description><see cref="HealthStatus.Degraded"/>: Mediator returns business logic failure</description></item>
///   <item><description><see cref="HealthStatus.Unhealthy"/>: Infrastructure exception or mediator unavailable</description></item>
/// </list>
/// </para>
/// <para>
/// Performance characteristics: The health check uses a minimal test query that executes
/// quickly without side effects. It includes metadata collection for diagnostic purposes
/// and integrates with ASP.NET Core health check middleware for monitoring and alerting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration in dependency injection
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck();
/// 
/// // Custom health check configuration
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck(
///     name: "mediator-infrastructure",
///     failureStatus: HealthStatus.Unhealthy,
///     tags: "mediator", "infrastructure"
///   );
/// 
/// // Health check endpoint configuration
/// app.MapHealthChecks("/health", new HealthCheckOptions {
///   Predicate = check =&gt; check.Tags.Contains("mediator"),
///   ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
/// });
/// 
/// // Programmatic health check execution
/// var healthCheckService = serviceProvider.GetRequiredService&lt;HealthCheckService&gt;();
/// var result = await healthCheckService.CheckHealthAsync();
/// 
/// if (result.Status == HealthStatus.Healthy) {
///   Console.WriteLine("Mediator is healthy");
/// }
/// </code>
/// </example>
/// <seealso cref="IHealthCheck"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="MediatorHealthCheckQuery"/>
/// <seealso cref="HealthCheckExtensions"/>
public class MediatorHealthCheck(
  IMediator mediator,
  ILogger<MediatorHealthCheck> logger,
  TimeProvider? timeProvider = null): IHealthCheck {

  private readonly IMediator _Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
  private readonly ILogger<MediatorHealthCheck> _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  private readonly TimeProvider _TimeProvider = timeProvider ?? TimeProvider.System;

  /// <summary>
  /// Performs the health check asynchronously by executing a test query through the mediator pipeline.
  /// </summary>
  /// <param name="context">The health check context containing registration information and tags.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the health check operation.</param>
  /// <returns>
  /// A task representing the asynchronous health check operation.
  /// The result contains the health status and diagnostic information:
  /// <list type="bullet">
  ///   <item><description><see cref="HealthStatus.Healthy"/>: Test query executed successfully</description></item>
  ///   <item><description><see cref="HealthStatus.Degraded"/>: Mediator returned business failure</description></item>
  ///   <item><description><see cref="HealthStatus.Unhealthy"/>: Infrastructure exception occurred</description></item>
  /// </list>
  /// </returns>
  /// <remarks>
  /// <para>
  /// Execution flow:
  /// <list type="number">
  ///   <item><description>Creates and executes <see cref="MediatorHealthCheckQuery"/> through mediator</description></item>
  ///   <item><description>Evaluates result success/failure status</description></item>
  ///   <item><description>Collects diagnostic metadata including timestamps and error details</description></item>
  ///   <item><description>Maps result to appropriate <see cref="HealthStatus"/> value</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Exception handling strategy: All exceptions are caught and converted to 
  /// <see cref="HealthStatus.Unhealthy"/> status, as any exception indicates a critical
  /// failure in the mediator infrastructure that prevents normal operation.
  /// </para>
  /// <para>
  /// Diagnostic data included:
  /// <list type="bullet">
  ///   <item><description>timestamp: UTC timestamp when check was performed</description></item>
  ///   <item><description>response: Success response data from test query</description></item>
  ///   <item><description>error_type/error_code: Failure details for business logic errors</description></item>
  ///   <item><description>exception_type: Exception type name for infrastructure errors</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Manual health check execution
  /// var healthCheck = new MediatorHealthCheck(mediator, logger);
  /// var context = new HealthCheckContext { Registration = registration };
  /// var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
  /// 
  /// switch (result.Status) {
  ///   case HealthStatus.Healthy:
  ///     Console.WriteLine($"Mediator healthy: {result.Description}");
  ///     break;
  ///   case HealthStatus.Degraded:
  ///     Console.WriteLine($"Mediator degraded: {result.Description}");
  ///     break;
  ///   case HealthStatus.Unhealthy:
  ///     Console.WriteLine($"Mediator unhealthy: {result.Description}");
  ///     if (result.Exception != null) {
  ///       Console.WriteLine($"Exception: {result.Exception}");
  ///     }
  ///     break;
  /// }
  /// </code>
  /// </example>
  [SuppressMessage(
    "Minor Code Smell",
    "S2221:\"Exception\" should not be caught",
    Justification = "Health checks must catch all exceptions to provide proper status reporting. Any exception indicates infrastructure failure and should result in Unhealthy status.")]
  public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default) {

    const string TimestampKey = "timestamp";

    try {
      _Logger.LogMediatorHealthCheckStarting();

      // Execute test query through mediator pipeline
      var healthCheckQuery = new MediatorHealthCheckQuery();
      var result = await _Mediator.ProcessAsync(healthCheckQuery, cancellationToken);

      if (result.IsSuccess) {
        _Logger.LogMediatorHealthCheckCompleted();
        return HealthCheckResult.Healthy(
          "Mediator is functioning normally",
          new Dictionary<string, object> {
            [TimestampKey] = _TimeProvider.GetUtcNow(),
            ["response"] = result.Value
          });
      } else {
        _Logger.LogMediatorHealthCheckFailed(result.Error);
        return HealthCheckResult.Degraded(
          $"Mediator test failed: {result.Error.Message}",
          null,
          new Dictionary<string, object> {
            [TimestampKey] = _TimeProvider.GetUtcNow(),
            ["error_type"] = result.Error.Type.ToString(),
            ["error_code"] = result.Error.Code,
            ["error_message"] = result.Error.Message
          });
      }
    } catch (OperationCanceledException) {
      _Logger.LogMediatorHealthCheckTimeout(_TimeProvider.GetUtcNow().Millisecond);
      return HealthCheckResult.Unhealthy(
        "Mediator health check was cancelled",
        null,
        new Dictionary<string, object> {
          [TimestampKey] = _TimeProvider.GetUtcNow(),
          ["cancelled"] = true
        });
    } catch (Exception ex) {
      _Logger.LogMediatorHealthCheckError(ex);
      return HealthCheckResult.Unhealthy(
        "Mediator is not responding",
        ex,
        new Dictionary<string, object> {
          [TimestampKey] = _TimeProvider.GetUtcNow(),
          ["exception_type"] = ex.GetType().Name,
          ["exception_message"] = ex.Message
        });
    }
  }
}
