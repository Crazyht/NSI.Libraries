using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator;

/// <summary>
/// Configuration options for the mediator infrastructure controlling behavior, performance, and operational characteristics.
/// </summary>
/// <remarks>
/// <para>
/// This class provides centralized configuration for all aspects of mediator operation including
/// performance tuning, operational behavior, logging configuration, and validation settings.
/// The options are designed to support both development and production scenarios with appropriate
/// defaults that balance performance, reliability, and diagnostics.
/// </para>
/// <para>
/// Configuration categories:
/// <list type="bullet">
///   <item><description>Performance settings: Timeout controls and concurrency limits for optimal throughput</description></item>
///   <item><description>Operational behavior: Handler validation and startup configuration options</description></item>
///   <item><description>Diagnostics: Detailed logging controls for troubleshooting and monitoring</description></item>
///   <item><description>Notification handling: Concurrency and parallel execution controls</description></item>
/// </list>
/// </para>
/// <para>
/// Integration with dependency injection:
/// <list type="bullet">
///   <item><description>Options pattern: Integrates with ASP.NET Core options system for configuration</description></item>
///   <item><description>Environment-specific: Supports different settings per environment (dev/staging/prod)</description></item>
///   <item><description>Hot reload: Changes can be applied at runtime through configuration providers</description></item>
///   <item><description>Validation: Built-in validation ensures configuration consistency and prevents runtime errors</description></item>
/// </list>
/// </para>
/// <para>
/// Performance considerations: Default values are optimized for typical workloads but can be
/// adjusted based on specific application requirements. Monitor application metrics to determine
/// optimal settings for your specific use case.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic registration with default options
/// services.AddMediator();
/// 
/// // Registration with custom configuration
/// services.AddMediator(options =&gt; {
///   options.DefaultTimeout = TimeSpan.FromMinutes(2);
///   options.EnableDetailedLogging = true;
///   options.MaxConcurrentNotificationHandlers = 20;
///   options.ValidateHandlersAtStartup = true;
/// });
/// 
/// // Configuration through appsettings.json
/// // appsettings.json:
/// // {
/// //   "Mediator": {
/// //     "DefaultTimeout": "00:01:30",
/// //     "EnableDetailedLogging": false,
/// //     "MaxConcurrentNotificationHandlers": 15,
/// //     "ValidateHandlersAtStartup": true
/// //   }
/// // }
/// 
/// // Register with configuration binding
/// services.Configure&lt;MediatorOptions&gt;(
///   configuration.GetSection("Mediator"));
/// services.AddMediator();
/// 
/// // Environment-specific configuration
/// services.AddMediator(options =&gt; {
///   if (environment.IsDevelopment()) {
///     options.EnableDetailedLogging = true;
///     options.DefaultTimeout = TimeSpan.FromMinutes(5); // Longer for debugging
///   } else {
///     options.EnableDetailedLogging = false;
///     options.DefaultTimeout = TimeSpan.FromSeconds(30);
///     options.MaxConcurrentNotificationHandlers = 50; // Higher for production
///   }
/// });
/// 
/// // Monitoring and health check integration
/// services.AddMediator(options =&gt; {
///   options.ValidateHandlersAtStartup = true; // Fail fast on misconfigurations
///   options.EnableDetailedLogging = configuration.GetValue&lt;bool&gt;("Monitoring:DetailedLogs");
/// });
/// 
/// // Usage in health checks and monitoring
/// services.AddHealthChecks()
///   .AddMediatorHealthCheck()
///   .AddCheck&lt;ConfigurationHealthCheck&gt;();
/// </code>
/// </example>
/// <seealso cref="MediatorImplementation"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="INotification"/>
public class MediatorOptions {

  /// <summary>
  /// Gets or sets the default timeout for request processing operations.
  /// </summary>
  /// <value>
  /// The default timeout duration applied to all request processing operations.
  /// Default value is 30 seconds. Must be a positive <see cref="TimeSpan"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// This timeout applies to individual request processing operations and affects:
  /// <list type="bullet">
  ///   <item><description>Handler execution: Maximum time allowed for a single handler to complete</description></item>
  ///   <item><description>Decorator pipeline: Total time for all decorators plus handler execution</description></item>
  ///   <item><description>Async operations: Timeout for async handler and decorator methods</description></item>
  ///   <item><description>Cancellation token: Default timeout when no explicit cancellation is provided</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Timeout behavior and considerations:
  /// <list type="bullet">
  ///   <item><description>Operation cancellation: Operations are cancelled gracefully when timeout is exceeded</description></item>
  ///   <item><description>Resource cleanup: Proper disposal and cleanup occurs even on timeout</description></item>
  ///   <item><description>Error handling: Timeouts result in ServiceUnavailable error through Result pattern</description></item>
  ///   <item><description>Performance impact: Very short timeouts may cause false positives during high load</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Recommended values by scenario:
  /// <list type="bullet">
  ///   <item><description>Web APIs: 15-30 seconds to prevent request timeouts</description></item>
  ///   <item><description>Background processing: 2-5 minutes for complex operations</description></item>
  ///   <item><description>Real-time systems: 5-10 seconds for responsive user experience</description></item>
  ///   <item><description>Batch processing: 10-30 minutes for large data operations</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Development environment - longer timeout for debugging
  /// options.DefaultTimeout = TimeSpan.FromMinutes(5);
  /// 
  /// // Production API - responsive timeout
  /// options.DefaultTimeout = TimeSpan.FromSeconds(15);
  /// 
  /// // Background service - extended timeout
  /// options.DefaultTimeout = TimeSpan.FromMinutes(2);
  /// 
  /// // Real-time system - aggressive timeout
  /// options.DefaultTimeout = TimeSpan.FromSeconds(10);
  /// </code>
  /// </example>
  public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  /// Gets or sets a value indicating whether to enable detailed logging for diagnostic and troubleshooting purposes.
  /// </summary>
  /// <value>
  /// <see langword="true"/> to enable detailed logging including performance metrics, handler resolution details,
  /// and pipeline execution traces; otherwise, <see langword="false"/> for standard operational logging.
  /// Default value is <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// Detailed logging includes comprehensive diagnostic information:
  /// <list type="bullet">
  ///   <item><description>Handler resolution: Logs handler discovery, registration validation, and DI resolution</description></item>
  ///   <item><description>Pipeline execution: Traces decorator chain construction and execution order</description></item>
  ///   <item><description>Performance metrics: Execution times, memory usage, and throughput statistics</description></item>
  ///   <item><description>Error diagnostics: Detailed exception information with full stack traces</description></item>
  ///   <item><description>Request correlation: Full request tracking with correlation IDs across the pipeline</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Performance and operational impact:
  /// <list type="bullet">
  ///   <item><description>Logging overhead: Additional CPU and memory usage for log generation and formatting</description></item>
  ///   <item><description>Storage requirements: Increased log volume requires more disk space and retention management</description></item>
  ///   <item><description>Network impact: Higher log shipping costs in centralized logging scenarios</description></item>
  ///   <item><description>Security considerations: Detailed logs may contain sensitive business data</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Recommended usage patterns:
  /// <list type="bullet">
  ///   <item><description>Development: Always enabled for debugging and development workflow</description></item>
  ///   <item><description>Staging: Enabled for integration testing and performance validation</description></item>
  ///   <item><description>Production: Selectively enabled during troubleshooting or monitoring periods</description></item>
  ///   <item><description>High-volume systems: Disabled by default, enabled on-demand for specific investigations</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Environment-based detailed logging
  /// options.EnableDetailedLogging = environment.IsDevelopment();
  /// 
  /// // Configuration-driven detailed logging
  /// options.EnableDetailedLogging = configuration.GetValue&lt;bool&gt;("Logging:Detailed");
  /// 
  /// // Conditional detailed logging for specific scenarios
  /// options.EnableDetailedLogging = configuration.GetValue&lt;bool&gt;("Features:TroubleshootingMode");
  /// 
  /// // Performance monitoring scenario
  /// if (configuration.GetValue&lt;bool&gt;("Monitoring:EnablePerformanceLogging")) {
  ///   options.EnableDetailedLogging = true;
  /// }
  /// </code>
  /// </example>
  public bool EnableDetailedLogging { get; set; }

  /// <summary>
  /// Gets or sets the maximum number of concurrent notification handlers that can execute simultaneously.
  /// </summary>
  /// <value>
  /// The maximum number of notification handlers that can run concurrently.
  /// Default value is 10. Must be a positive integer greater than 0.
  /// </value>
  /// <remarks>
  /// <para>
  /// Concurrency control and behavior:
  /// <list type="bullet">
  ///   <item><description>Parallel execution: Notification handlers execute in parallel up to the specified limit</description></item>
  ///   <item><description>Throttling mechanism: Additional handlers queue when limit is reached to prevent resource exhaustion</description></item>
  ///   <item><description>Resource protection: Prevents memory and CPU overload during high-volume notification scenarios</description></item>
  ///   <item><description>Error isolation: Failed handlers don't affect others due to parallel execution model</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Performance considerations and tuning:
  /// <list type="bullet">
  ///   <item><description>CPU cores: Optimal value typically correlates with available CPU cores (1-2x core count)</description></item>
  ///   <item><description>I/O bound operations: Higher values beneficial when handlers perform network or database operations</description></item>
  ///   <item><description>Memory usage: Each concurrent handler consumes memory; balance concurrency with available RAM</description></item>
  ///   <item><description>External dependencies: Consider rate limits and connection pools of external services</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Recommended values by workload type:
  /// <list type="bullet">
  ///   <item><description>CPU-intensive: 1x CPU core count (4-8 for typical servers)</description></item>
  ///   <item><description>I/O-intensive: 2-4x CPU core count (16-32 for typical servers)</description></item>
  ///   <item><description>Mixed workload: 1.5x CPU core count with monitoring and adjustment</description></item>
  ///   <item><description>High-volume systems: Start conservative (10-20) and scale based on metrics</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // CPU-bound notification handlers
  /// options.MaxConcurrentNotificationHandlers = Environment.ProcessorCount;
  /// 
  /// // I/O-bound notification handlers (email, webhooks, etc.)
  /// options.MaxConcurrentNotificationHandlers = Environment.ProcessorCount * 3;
  /// 
  /// // Configuration-driven concurrency
  /// options.MaxConcurrentNotificationHandlers = configuration.GetValue&lt;int&gt;("Performance:MaxConcurrentHandlers");
  /// 
  /// // Environment-specific concurrency
  /// options.MaxConcurrentNotificationHandlers = environment.IsProduction() ? 25 : 5;
  /// 
  /// // Adaptive concurrency based on system resources
  /// var availableMemoryGb = GC.GetTotalMemory(false) / (1024 * 1024 * 1024);
  /// options.MaxConcurrentNotificationHandlers = Math.Min(50, (int)(availableMemoryGb * 2));
  /// </code>
  /// </example>
  public int MaxConcurrentNotificationHandlers { get; set; } = 10;

  /// <summary>
  /// Gets or sets a value indicating whether to validate handler registration completeness at application startup.
  /// </summary>
  /// <value>
  /// <see langword="true"/> to perform comprehensive handler validation during application startup;
  /// otherwise, <see langword="false"/> to skip validation for faster startup.
  /// Default value is <see langword="true"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// Validation scope and behavior:
  /// <list type="bullet">
  ///   <item><description>Handler registration: Verifies all request types have corresponding registered handlers</description></item>
  ///   <item><description>Dependency validation: Ensures all handler dependencies can be resolved from DI container</description></item>
  ///   <item><description>Interface compliance: Validates handler implementations correctly implement required interfaces</description></item>
  ///   <item><description>Pipeline integrity: Checks decorator registration and compatibility with request types</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Startup impact and fail-fast behavior:
  /// <list type="bullet">
  ///   <item><description>Early detection: Configuration errors discovered at startup prevent runtime failures</description></item>
  ///   <item><description>Startup time: Adds validation overhead but prevents production issues</description></item>
  ///   <item><description>Fail-fast principle: Application fails to start if critical handlers are missing</description></item>
  ///   <item><description>Development feedback: Immediate feedback during development and deployment</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Recommended usage by environment:
  /// <list type="bullet">
  ///   <item><description>Development: Always enabled to catch registration issues early</description></item>
  ///   <item><description>CI/CD pipelines: Enabled to prevent deployment of misconfigured applications</description></item>
  ///   <item><description>Production: Enabled unless startup time is critical (microservices, serverless)</description></item>
  ///   <item><description>High-availability: May be disabled for faster startup in rolling deployments</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Always validate in non-production environments
  /// options.ValidateHandlersAtStartup = !environment.IsProduction();
  /// 
  /// // Configuration-driven validation
  /// options.ValidateHandlersAtStartup = configuration.GetValue("Startup:ValidateHandlers", true);
  /// 
  /// // Conditional validation based on deployment type
  /// options.ValidateHandlersAtStartup = !configuration.GetValue&lt;bool&gt;("Deployment:IsServerless");
  /// 
  /// // Development vs production behavior
  /// if (environment.IsDevelopment()) {
  ///   options.ValidateHandlersAtStartup = true; // Catch issues early
  /// } else if (environment.IsProduction()) {
  ///   options.ValidateHandlersAtStartup = configuration.GetValue("Features:StartupValidation", false);
  /// }
  /// 
  /// // Microservice scenario - prioritize startup speed
  /// options.ValidateHandlersAtStartup = !configuration.GetValue&lt;bool&gt;("Architecture:IsMicroservice");
  /// </code>
  /// </example>
  public bool ValidateHandlersAtStartup { get; set; } = true;
}
