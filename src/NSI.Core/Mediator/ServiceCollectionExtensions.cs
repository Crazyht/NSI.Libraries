using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator {
  /// <summary>
  /// Provides extension methods for registering mediator services with decorator support.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class contains extension methods for the dependency injection container
  /// to simplify registration of mediator, handlers, and decorators. It supports both
  /// automatic discovery and manual registration patterns.
  /// </para>
  /// <para>
  /// New features in this version:
  /// <list type="bullet">
  ///   <item><description>Automatic decorator discovery and registration</description></item>
  ///   <item><description>Support for decorator ordering and filtering</description></item>
  ///   <item><description>Enhanced validation with decorator compatibility checks</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds the mediator and automatically registers all handlers and decorators from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers and decorators.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or assemblies is null.</exception>
    /// <example>
    /// <code>
    /// // Register mediator with handlers and decorators from current assembly
    /// services.AddMediator(typeof(Program).Assembly);
    /// 
    /// // Register mediator with handlers and decorators from multiple assemblies
    /// services.AddMediator(
    ///   typeof(Application.AssemblyMarker).Assembly,
    ///   typeof(Infrastructure.AssemblyMarker).Assembly
    /// );
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
    /// Adds the mediator and automatically registers all handlers and decorators from the specified assemblies with options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure mediator options.</param>
    /// <param name="assemblies">The assemblies to scan for handlers and decorators.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services, configureOptions, or assemblies is null.</exception>
    /// <example>
    /// <code>
    /// services.AddMediator(options => {
    ///   options.DefaultTimeout = TimeSpan.FromSeconds(45);
    ///   options.EnableDetailedLogging = true;
    ///   options.MaxConcurrentNotificationHandlers = 20;
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
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <remarks>
    /// <para>
    /// Use this method when you want to register handlers and decorators manually or when you need
    /// more control over the registration process.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMediator()
    ///   .AddScoped&lt;IRequestHandler&lt;GetUserQuery, User&gt;, GetUserQueryHandler&gt;()
    ///   .AddScoped&lt;IRequestHandler&lt;CreateUserCommand, User&gt;, CreateUserCommandHandler&gt;()
    ///   .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
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
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure mediator options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null.</exception>
    /// <example>
    /// <code>
    /// services.AddMediator(options => {
    ///   options.DefaultTimeout = TimeSpan.FromMinutes(2);
    ///   options.ValidateHandlersAtStartup = false;
    /// })
    /// .AddScoped&lt;IRequestHandler&lt;GetUserQuery, User&gt;, GetUserQueryHandler&gt;()
    /// .AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(LoggingDecorator&lt;,&gt;));
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
    /// Adds common mediator decorators with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="includeLogging">Whether to include logging decorator.</param>
    /// <param name="includeValidation">Whether to include validation decorator.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    /// <example>
    /// <code>
    /// services.AddMediator(typeof(Program).Assembly)
    ///   .AddMediatorDecorators(includeLogging: true, includeValidation: true);
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
    /// Validates that all registered request types have corresponding handlers.
    /// </summary>
    /// <param name="serviceProvider">The service provider to validate.</param>
    /// <param name="assemblies">The assemblies containing request types to validate.</param>
    /// <returns>A validation result containing any missing handlers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider or assemblies is null.</exception>
    /// <example>
    /// <code>
    /// // In Program.cs after building the service provider
    /// var app = builder.Build();
    /// 
    /// var validationResult = app.Services.ValidateMediatorRegistration(typeof(Program).Assembly);
    /// if (!validationResult.IsValid) {
    ///   foreach (var error in validationResult.Errors) {
    ///     Console.WriteLine($"Missing handler: {error}");
    ///   }
    ///   throw new InvalidOperationException("Handler validation failed");
    /// }
    /// </code>
    /// </example>
    public static MediatorValidationResult ValidateMediatorRegistration(
      this IServiceProvider serviceProvider,
      params Assembly[] assemblies) {

      ArgumentNullException.ThrowIfNull(serviceProvider);
      ArgumentNullException.ThrowIfNull(assemblies);

      var result = new MediatorValidationResult();
      var logger = serviceProvider.GetService<ILogger<MediatorImplementation>>();

      foreach (var assembly in assemblies) {
        var requestTypes = GetRequestTypesFromAssembly(assembly);

        foreach (var requestType in requestTypes) {
          var responseType = GetResponseTypeFromRequest(requestType);
          if (responseType == null) {
            continue;
          }

          var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
          var handler = serviceProvider.GetService(handlerType);

          if (handler == null) {
            var error = $"No handler registered for request type '{requestType.Name}' expecting response type '{responseType.Name}'";
            result.Errors.Add(error);
            logger?.LogMediatorValidationNoHandler(error);
          } else {
            logger?.LogMediatorValidationSuccess(requestType.Name, handler.GetType());
          }
        }
      }

      result.IsValid = result.Errors.Count == 0;

      if (result.IsValid) {
        logger?.LogMediatorValidationGlobalSuccess(assemblies.SelectMany(GetRequestTypesFromAssembly).Count());
      } else {
        logger?.LogMediatorValidationFailure(result.Errors.Count);
      }

      return result;
    }

    /// <summary>
    /// Registers the core mediator services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void RegisterMediatorCore(IServiceCollection services) {
      // Register mediator as scoped service
      services.TryAddScoped<IMediator, MediatorImplementation>();

      // Register default options if not already configured
      services.TryAddSingleton<IConfigureOptions<MediatorOptions>>(
        new ConfigureOptions<MediatorOptions>(_ => { }));
    }

    /// <summary>
    /// Registers all handlers from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly) {
      var handlerTypes = assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
        .Where(t => t.GetInterfaces().Any(i => IsHandlerInterface(i)))
        .ToList();

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
    /// Registers all decorators from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    private static void RegisterDecoratorsFromAssembly(IServiceCollection services, Assembly assembly) {
      var decoratorTypes = assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
        .Where(t => t.GetInterfaces().Any(i => IsDecoratorInterface(i)))
        .ToList();

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
    /// Checks if a type is a handler interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the type is a handler interface; otherwise, <c>false</c>.</returns>
    private static bool IsHandlerInterface(Type type) {
      if (!type.IsGenericType) {
        return false;
      }

      var genericType = type.GetGenericTypeDefinition();
      return genericType == typeof(IRequestHandler<,>);
    }

    /// <summary>
    /// Checks if a type is a decorator interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the type is a decorator interface; otherwise, <c>false</c>.</returns>
    private static bool IsDecoratorInterface(Type type) {
      if (!type.IsGenericType) {
        return false;
      }

      var genericType = type.GetGenericTypeDefinition();
      return genericType == typeof(IRequestDecorator<,>);
    }

    /// <summary>
    /// Gets all request types from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>Collection of request types.</returns>
    private static IEnumerable<Type> GetRequestTypesFromAssembly(Assembly assembly) => [.. assembly.GetTypes()
          .Where(t => t.IsClass && !t.IsAbstract)
          .Where(t => t.GetInterfaces().Any(i =>
            i.IsGenericType &&
            typeof(IRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition())))];

    /// <summary>
    /// Gets the response type from a request type.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <returns>The response type, or null if not found.</returns>
    private static Type? GetResponseTypeFromRequest(Type requestType) {
      var requestInterface = Array.Find(requestType.GetInterfaces(), (i) => i.IsGenericType &&
          typeof(IRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition()));

      return requestInterface?.GetGenericArguments().FirstOrDefault();
    }
  }
}
