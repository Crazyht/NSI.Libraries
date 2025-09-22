using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NSI.Core.Mediator.HealthChecks;

/// <summary>
/// Extension methods for adding mediator health checks to the application.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides extension methods to integrate mediator health checks
/// with the ASP.NET Core health check system. The health checks validate that the
/// mediator infrastructure is properly configured and operational, including
/// handler registration, pipeline configuration, and service dependencies.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Automatic mediator infrastructure validation</description></item>
///   <item><description>Configurable health check naming and tagging</description></item>
///   <item><description>Flexible failure status reporting</description></item>
///   <item><description>Integration with ASP.NET Core health check middleware</description></item>
///   <item><description>Support for custom health check scenarios</description></item>
/// </list>
/// </para>
/// <para>
/// The health checks perform comprehensive validation including:
/// <list type="bullet">
///   <item><description>Handler registration verification</description></item>
///   <item><description>Decorator pipeline integrity validation</description></item>
///   <item><description>Service provider configuration checks</description></item>
///   <item><description>Request processing capability testing</description></item>
/// </list>
/// </para>
/// <para>
/// Performance considerations: Health checks are designed to be lightweight
/// and execute quickly during health check intervals. The validation process
/// uses cached metadata and minimal reflection to ensure optimal performance.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic mediator health check registration
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck();
/// 
/// // Custom health check with specific configuration
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck(
///     name: "mediator-core",
///     failureStatus: HealthStatus.Degraded,
///     tags: "mediator", "infrastructure", "core"
///   );
/// 
/// // Multiple health checks for different mediator aspects
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck("mediator-handlers", HealthStatus.Unhealthy, "mediator", "handlers")
///   .AddMediatorHealthCheck("mediator-pipeline", HealthStatus.Degraded, "mediator", "pipeline");
/// 
/// // Health check endpoint configuration
/// app.MapHealthChecks("/health", new HealthCheckOptions {
///   Predicate = check =&gt; check.Tags.Contains("mediator"),
///   ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
/// });
/// 
/// // Detailed health check with custom response
/// app.MapHealthChecks("/health/mediator", new HealthCheckOptions {
///   Predicate = check =&gt; check.Name.StartsWith("mediator"),
///   ResultStatusCodes = {
///     [HealthStatus.Healthy] = StatusCodes.Status200OK,
///     [HealthStatus.Degraded] = StatusCodes.Status200OK,
///     [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
///   }
/// });
/// </code>
/// </example>
/// <seealso cref="MediatorHealthCheck"/>
/// <seealso cref="IHealthChecksBuilder"/>
/// <seealso cref="HealthStatus"/>
public static class HealthCheckExtensions {
  /// <summary>
  /// Adds a health check for the mediator service infrastructure.
  /// </summary>
  /// <param name="builder">The health check builder to extend.</param>
  /// <param name="name">The name of the health check for identification and filtering.</param>
  /// <param name="failureStatus">
  /// The <see cref="HealthStatus"/> to report when the health check fails.
  /// Defaults to <see cref="HealthStatus.Unhealthy"/> if not specified.
  /// </param>
  /// <param name="tags">
  /// Optional tags to associate with the health check for grouping and filtering.
  /// If no tags are provided, defaults to a single "mediator" tag.
  /// </param>
  /// <returns>
  /// The <see cref="IHealthChecksBuilder"/> instance for method chaining.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="builder"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method registers a <see cref="MediatorHealthCheck"/> instance with the health check system.
  /// The health check validates the mediator infrastructure including:
  /// <list type="bullet">
  ///   <item><description>Handler registration: Verifies all required handlers are registered</description></item>
  ///   <item><description>Pipeline configuration: Validates decorator pipeline integrity</description></item>
  ///   <item><description>Service dependencies: Ensures all mediator dependencies are available</description></item>
  ///   <item><description>Processing capability: Tests basic request processing functionality</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Health check naming and tagging guidelines:
  /// <list type="bullet">
  ///   <item><description>Use descriptive names that identify the specific aspect being checked</description></item>
  ///   <item><description>Include "mediator" tag for filtering in health check endpoints</description></item>
  ///   <item><description>Add component-specific tags like "handlers", "pipeline", "infrastructure"</description></item>
  ///   <item><description>Use consistent naming patterns across related health checks</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Failure status recommendations:
  /// <list type="bullet">
  ///   <item><description><see cref="HealthStatus.Unhealthy"/>: Critical mediator failures that prevent operation</description></item>
  ///   <item><description><see cref="HealthStatus.Degraded"/>: Non-critical issues that may affect performance</description></item>
  ///   <item><description><see cref="HealthStatus.Healthy"/>: All mediator components operating normally</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Simple registration with defaults
  /// services.AddHealthChecks()
  ///   .AddMediatorHealthCheck();
  /// 
  /// // Production configuration with comprehensive monitoring
  /// services.AddHealthChecks()
  ///   .AddMediatorHealthCheck(
  ///     name: "mediator-infrastructure",
  ///     failureStatus: HealthStatus.Unhealthy,
  ///     tags: "mediator", "infrastructure", "critical"
  ///   )
  ///   .AddMediatorHealthCheck(
  ///     name: "mediator-performance", 
  ///     failureStatus: HealthStatus.Degraded,
  ///     tags: "mediator", "performance", "monitoring"
  ///   );
  /// 
  /// // Development environment with detailed checks
  /// if (env.IsDevelopment()) {
  ///   services.AddHealthChecks()
  ///     .AddMediatorHealthCheck("mediator-dev-full", tags: "mediator", "development", "detailed");
  /// }
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
