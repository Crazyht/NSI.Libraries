using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;

namespace NSI.Core.Mediator.HealthChecks;

/// <summary>
/// Handler for the mediator health check query that validates mediator infrastructure functionality.
/// </summary>
/// <remarks>
/// <para>
/// This handler provides a lightweight implementation specifically designed for health check purposes.
/// It validates that the complete mediator pipeline is operational by successfully processing the
/// health check query and returning a simple status response, exercising all core mediator components
/// including handler resolution, decorator execution, and response generation.
/// </para>
/// <para>
/// Architectural responsibilities:
/// <list type="bullet">
///   <item><description>Infrastructure validation: Confirms mediator pipeline can process requests end-to-end</description></item>
///   <item><description>Handler registration verification: Proves handlers can be resolved from DI container</description></item>
///   <item><description>Decorator pipeline testing: Ensures all registered decorators execute successfully</description></item>
///   <item><description>Response generation: Provides consistent health status information for monitoring</description></item>
/// </list>
/// </para>
/// <para>
/// Implementation characteristics:
/// <list type="bullet">
///   <item><description>Minimal processing: No business logic or side effects for fast execution</description></item>
///   <item><description>Always successful: Returns success to indicate infrastructure is operational</description></item>
///   <item><description>Stateless design: No dependencies or shared state for thread-safe execution</description></item>
///   <item><description>High-performance: Optimized for frequent health check intervals</description></item>
/// </list>
/// </para>
/// <para>
/// The handler is automatically registered with the dependency injection container when health check
/// services are configured through <see cref="HealthCheckExtensions"/>. It integrates seamlessly
/// with ASP.NET Core health check middleware and monitoring systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Automatic registration - no manual registration required
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck(); // Registers handler automatically
/// 
/// // Handler execution through mediator (internal usage)
/// var query = new MediatorHealthCheckQuery();
/// var result = await mediator.ProcessAsync(query, cancellationToken);
/// // Result: Success("Mediator is healthy")
/// 
/// // Integration with health check endpoint
/// app.MapHealthChecks("/health/mediator", new HealthCheckOptions {
///   Predicate = check =&gt; check.Tags.Contains("mediator"),
///   ResponseWriter = async (context, report) =&gt; {
///     var response = new {
///       status = report.Status.ToString(),
///       checks = report.Entries.Select(entry =&gt; new {
///         name = entry.Key,
///         status = entry.Value.Status.ToString(),
///         data = entry.Value.Data
///       })
///     };
///     context.Response.ContentType = "application/json";
///     await context.Response.WriteAsync(JsonSerializer.Serialize(response));
///   }
/// });
/// 
/// // Monitoring integration example
/// services.Configure&lt;HealthCheckPublisherOptions&gt;(options =&gt; {
///   options.Predicate = check =&gt; check.Tags.Contains("mediator");
///   options.Period = TimeSpan.FromSeconds(30);
/// });
/// </code>
/// </example>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="MediatorHealthCheckQuery"/>
/// <seealso cref="MediatorHealthCheck"/>
/// <seealso cref="HealthCheckExtensions"/>
/// <seealso cref="Result{T}"/>
public class MediatorHealthCheckQueryHandler: IRequestHandler<MediatorHealthCheckQuery, string> {

  /// <summary>
  /// Handles the health check query by returning a success status indicating mediator infrastructure is operational.
  /// </summary>
  /// <param name="request">The health check query request to process.</param>
  /// <param name="cancellationToken">The cancellation token for operation cancellation.</param>
  /// <returns>
  /// A task containing a successful <see cref="Result{T}"/> with the health status message "Mediator is healthy".
  /// This method always returns success to indicate that the mediator infrastructure is functional.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Execution behavior:
  /// <list type="bullet">
  ///   <item><description>Always successful: Never returns failure as successful execution proves infrastructure health</description></item>
  ///   <item><description>Immediate response: No async operations or external dependencies to minimize latency</description></item>
  ///   <item><description>Consistent message: Returns standardized "Mediator is healthy" for monitoring consistency</description></item>
  ///   <item><description>Pipeline validation: Successful execution validates entire mediator processing pipeline</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The method uses <see cref="Task.FromResult{TResult}(TResult)"/> for optimal performance since no actual
  /// async operations are performed. The successful completion of this handler proves that the mediator
  /// can resolve handlers, execute decorators, and generate responses correctly.
  /// </para>
  /// <para>
  /// Cancellation token handling: While the token is accepted for interface compliance, the operation
  /// is synchronous and completes immediately, so cancellation has no practical effect.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Internal usage by MediatorHealthCheck
  /// var handler = new MediatorHealthCheckQueryHandler();
  /// var query = new MediatorHealthCheckQuery();
  /// var result = await handler.HandleAsync(query, CancellationToken.None);
  /// 
  /// Assert.True(result.IsSuccess);
  /// Assert.Equal("Mediator is healthy", result.Value);
  /// 
  /// // Integration test example
  /// [Fact]
  /// public async Task HandleAsync_WithValidQuery_ShouldReturnSuccessWithHealthyMessage() {
  ///   // Arrange
  ///   var handler = new MediatorHealthCheckQueryHandler();
  ///   var query = new MediatorHealthCheckQuery();
  ///   
  ///   // Act
  ///   var result = await handler.HandleAsync(query, CancellationToken.None);
  ///   
  ///   // Assert
  ///   Assert.True(result.IsSuccess);
  ///   Assert.Equal("Mediator is healthy", result.Value);
  /// }
  /// </code>
  /// </example>
  public Task<Result<string>> HandleAsync(MediatorHealthCheckQuery request, CancellationToken cancellationToken = default)
    => Task.FromResult(Result.Success("Mediator is healthy"));
}
