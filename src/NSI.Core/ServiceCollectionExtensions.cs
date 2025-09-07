using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSI.Core.Identity;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core {
  /// <summary>
  /// Provides extension methods for <see cref="IServiceCollection"/> to register NSI Core services.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These extension methods simplify the registration of common services from the NSI Core library,
  /// including validation, user access, and other infrastructure components.
  /// </para>
  /// <para>
  /// The methods follow a fluent API pattern to allow method chaining during service registration.
  /// </para>
  /// </remarks>
  public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds daemon user accessor services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration section containing service user settings.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// <para>
    /// This method configures <see cref="ServiceUserSettings"/> and registers <see cref="DaemonUserAccessor"/> 
    /// as a singleton implementation of <see cref="IUserAccessor"/> for use in non-interactive contexts.
    /// </para>
    /// <para>
    /// This method automatically configures the <see cref="ServiceUserSettings"/> options from the 
    /// provided configuration section, simplifying setup compared to manual configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// Registration in service configuration:
    /// <code>
    /// public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    /// {
    ///     // Add daemon user accessor with configuration in a single call
    ///     services.AddDaemonUserAccessor(configuration.GetSection("ServiceUser"));
    ///     
    ///     // Other service registrations...
    /// }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.
    /// </exception>
    public static IServiceCollection AddDaemonUserAccessor(this IServiceCollection services, IConfiguration configuration) {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(configuration);

      // Binds the "ServiceUserSettings" section of appSettings.json to the options
      services.Configure<ServiceUserSettings>(options =>
          configuration.GetSection("ServiceUserSettings").Bind(options));

      // Registers the actual POCO as a singleton for direct injection
      services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ServiceUserSettings>>().Value);

      // Register the daemon user accessor as the IUserAccessor implementation
      services.AddSingleton<IUserAccessor, DaemonUserAccessor>();

      return services;
    }

    /// <summary>
    /// Registers all validator implementations from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for validators.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="assemblies"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method scans the specified assemblies for classes that implement
    /// <see cref="IValidator{T}"/> and registers them with the dependency injection
    /// container using scoped lifetime.
    /// </para>
    /// <para>
    /// If no assemblies are specified, the calling assembly is used by default.
    /// Classes are registered only if they aren't already registered (using TryAdd).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register validators from multiple assemblies
    /// services.AddValidators(
    ///     typeof(UserValidator).Assembly,
    ///     typeof(OrderValidator).Assembly
    /// );
    /// 
    /// // Register validators from the calling assembly
    /// services.AddValidators();
    /// </code>
    /// </example>
    public static IServiceCollection AddValidators(
      this IServiceCollection services,
      params Assembly[] assemblies) {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(assemblies);

      if (assemblies.Length == 0) {
        assemblies = [Assembly.GetCallingAssembly()];
      }

      var validatorInterface = typeof(IValidator<>);

      foreach (var assembly in assemblies) {
        var validators = assembly.GetTypes()
          .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
          .SelectMany(t => t.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
            .Select(i => new { Implementation = t, Interface = i }))
          .ToList();

        foreach (var validator in validators) {
          services.TryAddScoped(validator.Interface, validator.Implementation);
        }
      }

      return services;
    }

    /// <summary>
    /// Registers all validators from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type from the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is a convenience method that scans the assembly containing the specified
    /// marker type for validator implementations and registers them with the dependency
    /// injection container.
    /// </para>
    /// <para>
    /// Use this method when you want to register all validators from a specific assembly
    /// without explicitly passing the assembly instance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register all validators from the assembly containing UserValidator
    /// services.AddValidatorsFromAssemblyContaining&lt;UserValidator&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(
      this IServiceCollection services) {
      ArgumentNullException.ThrowIfNull(services);

      return services.AddValidators(typeof(T).Assembly);
    }

    /// <summary>
    /// Registers a specific validator implementation.
    /// </summary>
    /// <typeparam name="TModel">The model type being validated.</typeparam>
    /// <typeparam name="TValidator">The validator implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime (default is Scoped).</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="lifetime"/> is not a valid <see cref="ServiceLifetime"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers a specific validator implementation for a model type with
    /// the specified service lifetime. Unlike <see cref="AddValidators"/>, this method
    /// allows you to register a single validator with precise control over its lifetime.
    /// </para>
    /// <para>
    /// The default lifetime is Scoped, which is appropriate for most validators that
    /// may have dependencies on scoped services like database contexts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with default scoped lifetime
    /// services.AddValidator&lt;User, UserValidator&gt;();
    /// 
    /// // Register with singleton lifetime
    /// services.AddValidator&lt;Product, ProductValidator&gt;(ServiceLifetime.Singleton);
    /// </code>
    /// </example>
    public static IServiceCollection AddValidator<TModel, TValidator>(
          this IServiceCollection services,
          ServiceLifetime lifetime = ServiceLifetime.Scoped)
          where TValidator : class, IValidator<TModel> {
      ArgumentNullException.ThrowIfNull(services);

      return lifetime switch {
        ServiceLifetime.Singleton => services.AddSingleton<IValidator<TModel>, TValidator>(),
        ServiceLifetime.Scoped => services.AddScoped<IValidator<TModel>, TValidator>(),
        ServiceLifetime.Transient => services.AddTransient<IValidator<TModel>, TValidator>(),
        _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
      };
    }
  }
}
