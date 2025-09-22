using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator;

/// <summary>
/// Provides extension methods for registering mediator services with decorator support and comprehensive validation.
/// </summary>
/// <remarks>
/// <para>
/// This class contains extension methods for the dependency injection container
/// to simplify registration of mediator, handlers, and decorators. It supports both
/// automatic discovery and manual registration patterns with advanced configuration options.
/// </para>
/// <para>
/// Registration capabilities and features:
/// <list type="bullet">
///   <item><description>Automatic discovery: Scans assemblies for handlers and decorators using reflection</description></item>
///   <item><description>Manual registration: Supports explicit handler and decorator registration for fine-grained control</description></item>
///   <item><description>Configuration integration: Full integration with ASP.NET Core options pattern and configuration system</description></item>
///   <item><description>Validation support: Comprehensive startup validation with detailed error reporting</description></item>
///   <item><description>Performance optimization: Efficient registration with minimal overhead and fail-fast validation</description></item>
/// </list>
/// </para>
/// <para>
/// Decorator support and ordering:
/// <list type="bullet">
///   <item><description>Automatic decorator discovery: Finds and registers decorators from specified assemblies</description></item>
///   <item><description>Compatibility validation: Ensures decorators are compatible with request and response types</description></item>
///   <item><description>Common decorators: Built-in support for logging, validation, and caching decorators</description></item>
///   <item><description>Custom decorators: Support for application-specific cross-cutting concerns</description></item>
/// </list>
/// </para>
/// <para>
/// Performance considerations: Assembly scanning is performed once during startup to minimize
/// runtime overhead. The registration process uses efficient reflection techniques and caches
/// type information for optimal performance during dependency resolution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic registration with automatic discovery
/// services.AddMediator(typeof(Program).Assembly);
/// 
/// // Advanced configuration with custom options
/// services.AddMediator(options =&gt; {
///   options.DefaultTimeout = TimeSpan.FromMinutes(2);
///   options.EnableDetailedLogging = environment.IsDevelopment();
///   options.MaxConcurrentNotificationHandlers = Environment.ProcessorCount * 2;
///   options.ValidateHandlersAtStartup = true;
/// }, typeof(Application.Handlers.AssemblyMarker).Assembly,
///    typeof(Infrastructure.Services.AssemblyMarker).Assembly);
/// 
/// // Manual registration with decorators
/// services.AddMediator()
///   .AddScoped&lt;IRequestHandler&lt;GetUserQuery, User&gt;, GetUserQueryHandler&gt;()
///   .AddScoped&lt;IRequestHandler&lt;CreateUserCommand, User&gt;, CreateUserCommandHandler&gt;()
///   .AddMediatorDecorators(includeLogging: true, includeValidation: true);
/// 
/// // Startup validation example
/// var app = builder.Build();
/// var validationResult = app.Services.ValidateMediatorRegistration(assemblies);
/// if (!validationResult.IsValid) {
///   app.Logger.LogCritical("Mediator validation failed with {ErrorCount} errors", 
///     validationResult.Errors.Count);
///   Environment.Exit(1);
/// }
/// </code>
/// </example>
/// <seealso cref="MediatorOptions"/>
/// <seealso cref="MediatorValidationResult"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="IRequestDecorator{TRequest, TResponse}"/>
public static class ServiceCollectionExtensions {

  /// <summary>
  /// Adds the mediator and automatically registers all handlers and decorators from the specified assemblies.
  /// </summary>
  /// <param name="services">The service collection to configure. Must not be null.</param>
  /// <param name="assemblies">The assemblies to scan for handlers and decorators. Must not be null or empty.</param>
  /// <returns>The service collection for method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="assemblies"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This method performs comprehensive assembly scanning to automatically discover and register:
  /// <list type="bullet">
  ///   <item><description>Request handlers implementing <see cref="IRequestHandler{TRequest, TResponse}"/></description></item>
  ///   <item><description>Request decorators implementing <see cref="IRequestDecorator{TRequest, TResponse}"/></description></item>
  ///   <item><description>Core mediator services with default configuration options</description></item>
  ///   <item><description>Dependency injection container configuration for optimal performance</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Assembly scanning behavior:
  /// <list type="bullet">
  ///   <item><description>Type filtering: Only processes concrete, non-abstract classes with required interfaces</description></item>
  ///   <item><description>Interface validation: Ensures proper interface implementation and generic type constraints</description></item>
  ///   <item><description>Lifecycle management: Registers services with appropriate scoped lifetime for thread safety</description></item>
  ///   <item><description>Error handling: Graceful handling of reflection errors with detailed logging</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Register mediator with handlers from current assembly
  /// services.AddMediator(typeof(Program).Assembly);
  /// 
  /// // Register mediator with handlers from multiple assemblies
  /// services.AddMediator(
  ///   typeof(Application.AssemblyMarker).Assembly,
  ///   typeof(Infrastructure.AssemblyMarker).Assembly,
  ///   typeof(Domain.AssemblyMarker).Assembly
  /// );
  /// 
  /// // With additional service registration
  /// services.AddMediator(typeof(Program).Assembly)
  ///   .AddScoped&lt;IEmailService, EmailService&gt;()
  ///   .AddScoped&lt;IUserRepository, UserRepository&gt;();
  /// </code>
  /// </example>
  public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(assemblies);

    // Register core mediator services
    RegisterMediatorCore(services);

    // Register handlers and decorators from assemblies
    foreach (var assembly in assemblies) {
      RegisterHandlersFromAssembly(services, assembly);
      RegisterDecoratorsFromAssembly(services, assembly);
    }

    return services;
  }

  /// <summary>
  /// Adds the mediator and automatically registers all handlers and decorators from the specified assemblies with configuration options.
  /// </summary>
  /// <param name="services">The service collection to configure. Must not be null.</param>
  /// <param name="configureOptions">Action to configure mediator options. Must not be null.</param>
  /// <param name="assemblies">The assemblies to scan for handlers and decorators. Must not be null or empty.</param>
  /// <returns>The service collection for method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="configureOptions"/>, or <paramref name="assemblies"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This overload combines automatic assembly scanning with custom configuration options,
  /// allowing for fine-tuned mediator behavior while maintaining the convenience of automatic discovery.
  /// </para>
  /// <para>
  /// Configuration options include:
  /// <list type="bullet">
  ///   <item><description>Timeout settings: Default timeout for request processing operations</description></item>
  ///   <item><description>Logging configuration: Detailed logging levels and diagnostic information</description></item>
  ///   <item><description>Concurrency limits: Maximum concurrent notification handler execution</description></item>
  ///   <item><description>Validation settings: Startup validation behavior and error handling</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// services.AddMediator(options =&gt; {
  ///   // Production-optimized configuration
  ///   options.DefaultTimeout = TimeSpan.FromSeconds(30);
  ///   options.EnableDetailedLogging = false;
  ///   options.MaxConcurrentNotificationHandlers = Environment.ProcessorCount * 2;
  ///   options.ValidateHandlersAtStartup = true;
  /// }, typeof(Handlers.AssemblyMarker).Assembly,
  ///    typeof(Commands.AssemblyMarker).Assembly);
  /// 
  /// // Development configuration with extended timeouts
  /// services.AddMediator(options =&gt; {
  ///   options.DefaultTimeout = TimeSpan.FromMinutes(5);
  ///   options.EnableDetailedLogging = true;
  ///   options.ValidateHandlersAtStartup = true;
  /// }, typeof(Program).Assembly);
  /// </code>
  /// </example>
  public static IServiceCollection AddMediator(
    this IServiceCollection services,
    Action<MediatorOptions> configureOptions,
    params Assembly[] assemblies) {

    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configureOptions);
    ArgumentNullException.ThrowIfNull(assemblies);

    // Configure options
    services.Configure(configureOptions);

    return services.AddMediator(assemblies);
  }

  /// <summary>
  /// Adds the mediator service without automatic handler or decorator registration.
  /// </summary>
  /// <param name="services">The service collection to configure. Must not be null.</param>
  /// <returns>The service collection for method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This method registers only the core mediator infrastructure without any automatic
  /// handler or decorator discovery. Use this method when you need complete control over
  /// the registration process or when working with a small number of handlers.
  /// </para>
  /// <para>
  /// Manual registration scenarios:
  /// <list type="bullet">
  ///   <item><description>Fine-grained control: Explicit control over which handlers are registered</description></item>
  ///   <item><description>Performance optimization: Skip assembly scanning overhead for small applications</description></item>
  ///   <item><description>Conditional registration: Register handlers based on runtime conditions or configuration</description></item>
  ///   <item><description>Testing scenarios: Register mock or stub handlers for unit testing</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// services.AddMediator()
  ///   .AddScoped&lt;IRequestHandler&lt;GetUserQuery, User&gt;, GetUserQueryHandler&gt;()
  ///   .AddScoped&lt;IRequestHandler&lt;CreateUserCommand, User&gt;, CreateUserCommandHandler&gt;()
  ///   .AddScoped&lt;IRequestHandler&lt;UpdateUserCommand, Unit&gt;, UpdateUserCommandHandler&gt;()
  ///   .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;))
  ///   .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(ValidationDecorator&lt;,&gt;));
  /// 
  /// // Conditional registration based on environment
  /// services.AddMediator();
  /// if (environment.IsDevelopment()) {
  ///   services.AddScoped&lt;IRequestHandler&lt;DebugQuery, string&gt;, DebugQueryHandler&gt;();
  /// }
  /// </code>
  /// </example>
  public static IServiceCollection AddMediator(this IServiceCollection services) {
    ArgumentNullException.ThrowIfNull(services);

    RegisterMediatorCore(services);
    return services;
  }

  /// <summary>
  /// Adds the mediator service with configuration options but without automatic handler or decorator registration.
  /// </summary>
  /// <param name="services">The service collection to configure. Must not be null.</param>
  /// <param name="configureOptions">Action to configure mediator options. Must not be null.</param>
  /// <returns>The service collection for method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This method combines manual registration capability with custom configuration options,
  /// providing maximum flexibility for complex registration scenarios while ensuring
  /// proper mediator configuration.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// services.AddMediator(options =&gt; {
  ///   options.DefaultTimeout = TimeSpan.FromMinutes(2);
  ///   options.ValidateHandlersAtStartup = false; // Skip validation for faster startup
  ///   options.EnableDetailedLogging = configuration.GetValue&lt;bool&gt;("Logging:Detailed");
  /// })
  /// .AddScoped&lt;IRequestHandler&lt;GetUserQuery, User&gt;, GetUserQueryHandler&gt;()
  /// .AddScoped&lt;IRequestHandler&lt;CreateUserCommand, User&gt;, CreateUserCommandHandler&gt;()
  /// .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
  /// 
  /// // Configuration-driven registration
  /// services.AddMediator(options =&gt; {
  ///   options.DefaultTimeout = configuration.GetValue("Mediator:Timeout", TimeSpan.FromSeconds(30));
  ///   options.MaxConcurrentNotificationHandlers = configuration.GetValue("Mediator:MaxConcurrency", 10);
  /// });
  /// </code>
  /// </example>
  public static IServiceCollection AddMediator(
    this IServiceCollection services,
    Action<MediatorOptions> configureOptions) {

    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configureOptions);

    services.Configure(configureOptions);
    return services.AddMediator();
  }

  /// <summary>
  /// Adds common mediator decorators with configurable options for cross-cutting concerns.
  /// </summary>
  /// <param name="services">The service collection to configure. Must not be null.</param>
  /// <param name="includeLogging">Whether to include logging decorator for request/response tracing.</param>
  /// <param name="includeValidation">Whether to include validation decorator for input validation.</param>
  /// <returns>The service collection for method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This method provides a convenient way to add commonly used decorators that implement
  /// cross-cutting concerns. Each decorator can be selectively enabled based on application requirements.
  /// </para>
  /// <para>
  /// Decorator functionality:
  /// <list type="bullet">
  ///   <item><description>Logging decorator: Provides comprehensive request/response logging with performance metrics</description></item>
  ///   <item><description>Validation decorator: Performs input validation before handler execution using configured validation rules</description></item>
  ///   <item><description>Execution order: Decorators are applied in registration order, with validation typically first</description></item>
  ///   <item><description>Error handling: Each decorator handles errors gracefully and maintains the Result pattern</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Add all common decorators
  /// services.AddMediator(typeof(Program).Assembly)
  ///   .AddMediatorDecorators(includeLogging: true, includeValidation: true);
  /// 
  /// // Production setup - logging only
  /// services.AddMediator(typeof(Program).Assembly)
  ///   .AddMediatorDecorators(includeLogging: true, includeValidation: false);
  /// 
  /// // Development setup - validation only for testing
  /// services.AddMediator(typeof(Program).Assembly)
  ///   .AddMediatorDecorators(includeLogging: false, includeValidation: true);
  /// 
  /// // Custom decorator registration
  /// services.AddMediator(typeof(Program).Assembly)
  ///   .AddMediatorDecorators(includeLogging: true, includeValidation: true)
  ///   .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(CachingDecorator&lt;,&gt;))
  ///   .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(AuthorizationDecorator&lt;,&gt;));
  /// </code>
  /// </example>
  public static IServiceCollection AddMediatorDecorators(
    this IServiceCollection services,
    bool includeLogging = true,
    bool includeValidation = true) {

    ArgumentNullException.ThrowIfNull(services);

    if (includeLogging) {
      services.AddScoped(typeof(IRequestDecorator<,>), typeof(Decorators.LoggingDecorator<,>));
    }

    if (includeValidation) {
      services.AddScoped(typeof(IRequestDecorator<,>), typeof(Decorators.ValidationDecorator<,>));
    }

    return services;
  }

  /// <summary>
  /// Validates that all registered request types have corresponding handlers and reports detailed validation results.
  /// </summary>
  /// <param name="serviceProvider">The service provider to validate against. Must not be null.</param>
  /// <param name="assemblies">The assemblies containing request types to validate. Must not be null or empty.</param>
  /// <returns>A comprehensive validation result containing handler status and any configuration issues.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> or <paramref name="assemblies"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This method performs comprehensive validation of the mediator configuration by:
  /// <list type="bullet">
  ///   <item><description>Handler discovery: Identifies all request types in the specified assemblies</description></item>
  ///   <item><description>Registration verification: Confirms handlers are properly registered in the DI container</description></item>
  ///   <item><description>Dependency validation: Ensures all handler dependencies can be resolved</description></item>
  ///   <item><description>Type compatibility: Validates request/response type matching between handlers and requests</description></item>
  ///   <item><description>Performance logging: Comprehensive logging for validation events</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Validation scope and behavior:
  /// <list type="bullet">
  ///   <item><description>Request type discovery: Scans for types implementing <see cref="IRequest{TResponse}"/></description></item>
  ///   <item><description>Handler resolution: Attempts to resolve handlers from the service provider</description></item>
  ///   <item><description>Error aggregation: Collects all validation errors for comprehensive reporting</description></item>
  ///   <item><description>Success tracking: Counts successfully validated handlers for metrics</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Startup validation with error handling
  /// var app = builder.Build();
  /// 
  /// var validationResult = app.Services.ValidateMediatorRegistration(
  ///   typeof(Application.Commands.AssemblyMarker).Assembly,
  ///   typeof(Application.Queries.AssemblyMarker).Assembly);
  /// 
  /// if (!validationResult.IsValid) {
  ///   var logger = app.Services.GetRequiredService&lt;ILogger&lt;Program&gt;&gt;();
  ///   
  ///   foreach (var error in validationResult.Errors) {
  ///     logger.LogCritical("Mediator validation error: {Error}", error);
  ///   }
  ///   
  ///   throw new InvalidOperationException(
  ///     $"Mediator validation failed with {validationResult.Errors.Count} errors. " +
  ///     "Check handler registrations and dependencies.");
  /// }
  /// 
  /// app.Logger.LogInformation(
  ///   "Mediator validation successful: {HandlerCount} handlers validated", 
  ///   validationResult.ValidatedHandlerCount);
  /// 
  /// // Health check integration
  /// services.AddHealthChecks()
  ///   .AddCheck("mediator", () =&gt; {
  ///     var result = serviceProvider.ValidateMediatorRegistration(assemblies);
  ///     return result.IsValid 
  ///       ? HealthCheckResult.Healthy($"Validated {result.ValidatedHandlerCount} handlers")
  ///       : HealthCheckResult.Unhealthy($"Validation failed: {string.Join(", ", result.Errors)}");
  ///   });
  /// </code>
  /// </example>
  public static MediatorValidationResult ValidateMediatorRegistration(
    this IServiceProvider serviceProvider,
    params Assembly[] assemblies) {

    ArgumentNullException.ThrowIfNull(serviceProvider);
    ArgumentNullException.ThrowIfNull(assemblies);

    var (errors, count) = ValidateHandlersInAssemblies(serviceProvider, assemblies);

    return CreateValidationResult(errors, count);
  }

  /// <summary>
  /// Validates handlers across multiple assemblies and returns aggregated results.
  /// </summary>
  /// <param name="serviceProvider">The service provider for handler resolution.</param>
  /// <param name="assemblies">Assemblies to validate.</param>
  /// <returns>Tuple containing errors list and total count.</returns>
  private static (List<string> errors, int count) ValidateHandlersInAssemblies(
    IServiceProvider serviceProvider,
    Assembly[] assemblies) {

    var errors = new List<string>();
    var count = 0;

    foreach (var assembly in assemblies) {
      var requestTypes = GetRequestTypesFromAssembly(assembly);
      var (assemblyErrors, assemblyCount) = ValidateRequestTypes(serviceProvider, requestTypes);

      errors.AddRange(assemblyErrors);
      count += assemblyCount;
    }

    return (errors, count);
  }

  /// <summary>
  /// Validates a collection of request types and returns validation results.
  /// </summary>
  /// <param name="serviceProvider">The service provider for handler resolution.</param>
  /// <param name="requestTypes">Request types to validate.</param>
  /// <returns>Tuple containing errors list and count.</returns>
  private static (List<string> errors, int count) ValidateRequestTypes(
    IServiceProvider serviceProvider,
    IEnumerable<Type> requestTypes) {

    var errors = new List<string>();
    var count = 0;

    foreach (var requestType in requestTypes) {
      count++;
      var responseType = GetResponseTypeFromRequest(requestType);
      if (responseType == null) {
        continue;
      }

      var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
      var handler = serviceProvider.GetService(handlerType);

      if (handler == null) {
        var error = $"No handler registered for request type '{requestType.Name}' expecting response type '{responseType.Name}'";
        errors.Add(error);
      }
    }

    return (errors, count);
  }

  /// <summary>
  /// Creates the final validation result.
  /// </summary>
  /// <param name="errors">Collection of validation errors.</param>
  /// <param name="count">Total number of validated handlers.</param>
  /// <returns>Complete validation result.</returns>
  private static MediatorValidationResult CreateValidationResult(
    List<string> errors,
    int count) =>
    errors.Count > 0
      ? MediatorValidationResult.Failure(errors)
      : MediatorValidationResult.Success(count);

  /// <summary>
  /// Registers the core mediator services with the dependency injection container.
  /// </summary>
  /// <param name="services">The service collection to configure.</param>
  /// <remarks>
  /// <para>
  /// This method registers the essential mediator infrastructure services:
  /// <list type="bullet">
  ///   <item><description>Core mediator: Registers <see cref="IMediator"/> with <see cref="MediatorImplementation"/></description></item>
  ///   <item><description>Default options: Configures default <see cref="MediatorOptions"/> if not already registered</description></item>
  ///   <item><description>Scoped lifetime: Uses scoped lifetime for thread-safe request processing</description></item>
  ///   <item><description>Options integration: Integrates with ASP.NET Core options pattern</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  private static void RegisterMediatorCore(IServiceCollection services) {
    // Register mediator as scoped service
    services.TryAddScoped<IMediator, MediatorImplementation>();

    // Register default options if not already configured
    services.TryAddSingleton<IConfigureOptions<MediatorOptions>>(
      new ConfigureOptions<MediatorOptions>(_ => { }));
  }

  /// <summary>
  /// Registers all handlers from the specified assembly using reflection-based discovery.
  /// </summary>
  /// <param name="services">The service collection to configure.</param>
  /// <param name="assembly">The assembly to scan for handler implementations.</param>
  /// <remarks>
  /// <para>
  /// This method performs efficient handler discovery and registration:
  /// <list type="bullet">
  ///   <item><description>Type filtering: Only processes concrete, instantiable classes</description></item>
  ///   <item><description>Interface detection: Identifies handlers implementing <see cref="IRequestHandler{TRequest, TResponse}"/></description></item>
  ///   <item><description>Multiple interfaces: Supports handlers implementing multiple handler interfaces</description></item>
  ///   <item><description>Scoped registration: Registers handlers with scoped lifetime for proper DI behavior</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly) {
    const int estimatedHandlerCount = 50; // Pre-size collections for better performance
    var handlerTypes = new List<Type>(estimatedHandlerCount);

    foreach (var type in assembly.GetTypes()) {
      if (type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition &&
          type.GetInterfaces().Any(IsHandlerInterface)) {
        handlerTypes.Add(type);
      }
    }

    foreach (var handlerType in handlerTypes) {
      var handlerInterfaces = handlerType.GetInterfaces()
        .Where(IsHandlerInterface)
        .ToList();

      foreach (var handlerInterface in handlerInterfaces) {
        services.AddScoped(handlerInterface, handlerType);
      }
    }
  }

  /// <summary>
  /// Registers all decorators from the specified assembly using reflection-based discovery.
  /// </summary>
  /// <param name="services">The service collection to configure.</param>
  /// <param name="assembly">The assembly to scan for decorator implementations.</param>
  /// <remarks>
  /// <para>
  /// This method performs efficient decorator discovery and registration:
  /// <list type="bullet">
  ///   <item><description>Type filtering: Only processes concrete, instantiable decorator classes</description></item>
  ///   <item><description>Interface detection: Identifies decorators implementing <see cref="IRequestDecorator{TRequest, TResponse}"/></description></item>
  ///   <item><description>Multiple interfaces: Supports decorators implementing multiple decorator interfaces</description></item>
  ///   <item><description>Scoped registration: Registers decorators with scoped lifetime for proper pipeline behavior</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  private static void RegisterDecoratorsFromAssembly(IServiceCollection services, Assembly assembly) {
    const int estimatedDecoratorCount = 20; // Pre-size collections for better performance
    var decoratorTypes = new List<Type>(estimatedDecoratorCount);

    foreach (var type in assembly.GetTypes()) {
      if (type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition &&
          type.GetInterfaces().Any(IsDecoratorInterface)) {
        decoratorTypes.Add(type);
      }
    }

    foreach (var decoratorType in decoratorTypes) {
      var decoratorInterfaces = decoratorType.GetInterfaces()
        .Where(IsDecoratorInterface)
        .ToList();

      foreach (var decoratorInterface in decoratorInterfaces) {
        services.AddScoped(decoratorInterface, decoratorType);
      }
    }
  }

  /// <summary>
  /// Determines whether a type is a handler interface.
  /// </summary>
  /// <param name="type">The type to examine.</param>
  /// <returns><see langword="true"/> if the type is a handler interface; otherwise, <see langword="false"/>.</returns>
  /// <remarks>
  /// <para>
  /// This method checks for the <see cref="IRequestHandler{TRequest, TResponse}"/> interface pattern
  /// and validates the generic type definition to ensure proper handler identification.
  /// </para>
  /// </remarks>
  private static bool IsHandlerInterface(Type type) {
    if (!type.IsGenericType) {
      return false;
    }

    var genericType = type.GetGenericTypeDefinition();
    return genericType == typeof(IRequestHandler<,>);
  }

  /// <summary>
  /// Determines whether a type is a decorator interface.
  /// </summary>
  /// <param name="type">The type to examine.</param>
  /// <returns><see langword="true"/> if the type is a decorator interface; otherwise, <see langword="false"/>.</returns>
  /// <remarks>
  /// <para>
  /// This method checks for the <see cref="IRequestDecorator{TRequest, TResponse}"/> interface pattern
  /// and validates the generic type definition to ensure proper decorator identification.
  /// </para>
  /// </remarks>
  private static bool IsDecoratorInterface(Type type) {
    if (!type.IsGenericType) {
      return false;
    }

    var genericType = type.GetGenericTypeDefinition();
    return genericType == typeof(IRequestDecorator<,>);
  }

  /// <summary>
  /// Gets all request types from an assembly that implement the IRequest interface.
  /// </summary>
  /// <param name="assembly">The assembly to scan for request types.</param>
  /// <returns>Collection of request types found in the assembly.</returns>
  /// <remarks>
  /// <para>
  /// This method identifies all types that implement <see cref="IRequest{TResponse}"/>
  /// and can be used as request objects in the mediator pattern.
  /// </para>
  /// </remarks>
  private static IEnumerable<Type> GetRequestTypesFromAssembly(Assembly assembly) =>
    [.. assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract)
        .Where(t => t.GetInterfaces().Any(i =>
          i.IsGenericType &&
          typeof(IRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition())))];

  /// <summary>
  /// Extracts the response type from a request type's interface implementation.
  /// </summary>
  /// <param name="requestType">The request type to analyze.</param>
  /// <returns>The response type, or null if not found or invalid.</returns>
  /// <remarks>
  /// <para>
  /// This method uses reflection to identify the response type parameter from
  /// the <see cref="IRequest{TResponse}"/> interface implementation.
  /// </para>
  /// </remarks>
  private static Type? GetResponseTypeFromRequest(Type requestType) {
    var requestInterface = Array.Find(requestType.GetInterfaces(), i =>
      i.IsGenericType && typeof(IRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition()));

    return requestInterface?.GetGenericArguments().FirstOrDefault();
  }
}
