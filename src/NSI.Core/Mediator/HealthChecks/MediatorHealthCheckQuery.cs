using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator.HealthChecks;

/// <summary>
/// Simple health check query for testing mediator functionality and infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// This query is designed specifically for health check purposes and provides a lightweight
/// mechanism to validate that the complete mediator pipeline is operational. It exercises
/// the entire request processing workflow including handler resolution, decorator execution,
/// and response generation without performing any business operations or side effects.
/// </para>
/// <para>
/// Architectural purpose:
/// <list type="bullet">
///   <item><description>Handler registration validation: Ensures handlers can be resolved from DI container</description></item>
///   <item><description>Pipeline integrity testing: Validates all decorators execute correctly</description></item>
///   <item><description>Service dependency verification: Confirms all required services are available</description></item>
///   <item><description>Infrastructure monitoring: Provides early detection of mediator configuration issues</description></item>
/// </list>
/// </para>
/// <para>
/// Usage considerations:
/// <list type="bullet">
///   <item><description>Internal use only: Not intended for application business logic</description></item>
///   <item><description>Lightweight execution: Minimal processing overhead for frequent health checks</description></item>
///   <item><description>No side effects: Safe to execute repeatedly without state changes</description></item>
///   <item><description>Fast response: Designed for quick health check intervals</description></item>
/// </list>
/// </para>
/// <para>
/// The query returns a simple string response indicating mediator operational status.
/// This approach keeps the health check implementation minimal while still validating
/// the complete request processing pipeline through all registered decorators and handlers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Internal usage by MediatorHealthCheck - not for application code
/// var healthQuery = new MediatorHealthCheckQuery();
/// var result = await mediator.ProcessAsync(healthQuery, cancellationToken);
/// 
/// if (result.IsSuccess) {
///   Console.WriteLine($"Mediator health: {result.Value}"); // "Mediator is healthy"
/// }
/// 
/// // Integration with health check system
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck("mediator-infrastructure");
/// 
/// // Health check endpoint
/// app.MapHealthChecks("/health/mediator", new HealthCheckOptions {
///   Predicate = check =&gt; check.Tags.Contains("mediator")
/// });
/// </code>
/// </example>
/// <seealso cref="IQuery{TResponse}"/>
/// <seealso cref="MediatorHealthCheck"/>
/// <seealso cref="MediatorHealthCheckQueryHandler"/>
/// <seealso cref="HealthCheckExtensions"/>
public record MediatorHealthCheckQuery: IQuery<string>;
