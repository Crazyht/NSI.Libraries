using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator.HealthChecks;
/// <summary>
/// Health check for the mediator service.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that the mediator is functioning correctly by
/// executing a simple test query. It helps ensure that the mediator and its
/// dependencies are properly configured and operational.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="MediatorHealthCheck"/> class.
/// </remarks>
/// <param name="mediator">The mediator instance to check.</param>
/// <param name="logger">The logger for diagnostic information.</param>
/// <param name="timeProvider">Provider for time operations. If null, <see cref="TimeProvider.System"/> will be used.</param>
/// <exception cref="ArgumentNullException">
/// Thrown when mediator or logger is null.
/// </exception>
public class MediatorHealthCheck(IMediator mediator, ILogger<MediatorHealthCheck> logger, TimeProvider? timeProvider = null): IHealthCheck {
  private readonly IMediator _Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
  private readonly ILogger<MediatorHealthCheck> _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  private readonly TimeProvider _TimeProvider = timeProvider ?? TimeProvider.System;

  /// <summary>
  /// Performs the health check asynchronously.
  /// </summary>
  /// <param name="context">The health check context.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>
  /// A task representing the health check result, which will be:
  /// <list type="bullet">
  ///   <item><description>Healthy when the mediator successfully processes the test query</description></item>
  ///   <item><description>Degraded when the mediator returns a failure result</description></item>
  ///   <item><description>Unhealthy when an exception occurs during processing</description></item>
  /// </list>
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method executes a simple <see cref="MediatorHealthCheckQuery"/> to verify that
  /// the mediator pipeline is working correctly, including all registered handlers and decorators.
  /// </para>
  /// <para>
  /// Any exception during processing is caught and results in an Unhealthy status, as
  /// this indicates a critical failure in the mediator infrastructure.
  /// </para>
  /// </remarks>
  [SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught", Justification = "All \"Exception\" should be considered as Unhealthy.")]
  public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default) {

    try {
      _Logger.LogMediatorHealthCheckStarting();

      // Test with a simple health check query
      var healthCheckQuery = new MediatorHealthCheckQuery();
      var result = await _Mediator.ProcessAsync(healthCheckQuery, cancellationToken);

      if (result.IsSuccess) {
        _Logger.LogMediatorHealthCheckStarted();
        return HealthCheckResult.Healthy("Mediator is functioning normally",
          new Dictionary<string, object> {
            ["timestamp"] = _TimeProvider.GetUtcNow(),
            ["response"] = result.Value
          });
      } else {
        _Logger.LogMediatorHealthCheckFailed(result.Error);
        return HealthCheckResult.Degraded($"Mediator test failed: {result.Error.Message}",
          null,
          new Dictionary<string, object> {
            ["timestamp"] = _TimeProvider.GetUtcNow(),
            ["error_type"] = result.Error.Type.ToString(),
            ["error_code"] = result.Error.Code
          });
      }
    } catch (Exception ex) {
      _Logger.LogMediatorHealthCheckError(ex);
      return HealthCheckResult.Unhealthy("Mediator is not responding", ex,
        new Dictionary<string, object> {
          ["timestamp"] = _TimeProvider.GetUtcNow(),
          ["exception_type"] = ex.GetType().Name
        });
    }
  }
}
