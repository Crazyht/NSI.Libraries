using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NSI.Core.Mediator.HealthChecks {
  /// <summary>
  /// Extension methods for adding mediator health checks.
  /// </summary>
  public static class HealthCheckExtensions {
    /// <summary>
    /// Adds a health check for the mediator service.
    /// </summary>
    /// <param name="builder">The health check builder.</param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="failureStatus">The status to report when the check fails.</param>
    /// <param name="tags">Tags to associate with the health check.</param>
    /// <returns>The health check builder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///   .AddMediatorHealthCheck()
    ///   .AddMediatorHealthCheck("mediator-custom", HealthStatus.Degraded, "mediator", "infrastructure");
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddMediatorHealthCheck(
      this IHealthChecksBuilder builder,
      string name = "mediator",
      HealthStatus? failureStatus = null,
      params string[] tags) {

      ArgumentNullException.ThrowIfNull(builder);

      return builder.AddCheck<MediatorHealthCheck>(
        name,
        failureStatus,
        tags?.Length > 0 ? tags : ["mediator"]);
    }
  }
}
