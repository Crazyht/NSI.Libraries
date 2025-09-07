using Microsoft.Extensions.DependencyInjection;
using NSI.AspNetCore.Identity;
using NSI.Core.Identity;

namespace NSI.AspNetCore {
  /// <summary>
  /// Provides extension methods for <see cref="IServiceCollection"/> to register NSI services in ASP.NET Core applications.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class contains extension methods that simplify the registration of NSI's infrastructure services
  /// with ASP.NET Core's dependency injection system. It offers different registration strategies for
  /// various application scenarios.
  /// </para>
  /// <para>
  /// The extension methods provided by this class include:
  /// <list type="bullet">
  ///   <item>
  ///     <description><see cref="AddNsiIdentity"/>: Registers standard user identity services for web applications.</description>
  ///   </item>
  ///   <item>
  ///     <description><see cref="AddHybridUserAccessor"/>: Registers a hybrid user accessor that works in both web and non-web contexts.</description>
  ///   </item>
  /// </list>
  /// </para>
  /// <para>
  /// These methods help ensure consistent service registration across applications and reduce the
  /// boilerplate code needed to set up the NSI infrastructure.
  /// </para>
  /// </remarks>
  /// <example>
  /// Using the extension methods in Startup.ConfigureServices:
  /// <code>
  /// public void ConfigureServices(IServiceCollection services)
  /// {
  ///     // For web applications with authenticated users only
  ///     services.AddNsiIdentity();
  ///     
  ///     // Or, for applications that need to work in both web and non-web contexts
  ///     services.Configure&lt;ServiceUserSettings&gt;(Configuration.GetSection("ServiceUser"));
  ///     services.AddHybridUserAccessor();
  ///     
  ///     // Other service registrations...
  /// }
  /// </code>
  /// </example>
  public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds the necessary services for NSI Identity to the ASP.NET Core service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// <para>
    /// This method registers core identity services required for ASP.NET Core applications, including:
    /// <list type="bullet">
    ///   <item><description>HTTP context accessor for accessing the current HTTP context</description></item>
    ///   <item><description>User accessor that retrieves user information from HTTP context claims</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use this method when your application only needs to access user information from authenticated
    /// HTTP requests. For applications that also need to support non-interactive contexts,
    /// consider using <see cref="AddHybridUserAccessor"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// Registration in Startup.ConfigureServices:
    /// <code>
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     // Add standard identity services for web applications
    ///     services.AddNsiIdentity();
    ///     
    ///     // Other service registrations...
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddNsiIdentity(this IServiceCollection services) {
      // Register the HTTP context accessor
      services.AddHttpContextAccessor();
      // Register the user accessor
      services.AddScoped<IUserAccessor, HttpContextUserAccessor>();

      return services;
    }

    /// <summary>
    /// Adds a hybrid user accessor that tries to use HTTP context first and falls back to daemon user.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// <para>
    /// This method registers a hybrid <see cref="IUserAccessor"/> implementation that first attempts 
    /// to get user information from the HTTP context, and if that fails, falls back to using 
    /// a daemon user from configuration.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> This registration requires <see cref="ServiceUserSettings"/> to be configured 
    /// via <c>services.Configure&lt;ServiceUserSettings&gt;()</c> for the fallback mechanism to work correctly.
    /// </para>
    /// <para>
    /// This approach is useful for components that need to work in both web and non-web contexts,
    /// ensuring there's always a valid user for audit tracking and authorization.
    /// </para>
    /// <para>
    /// When using this method, do not register <see cref="HttpContextUserAccessor"/> or <see cref="DaemonUserAccessor"/>
    /// directly as implementations of <see cref="IUserAccessor"/>, as they are registered internally.
    /// </para>
    /// </remarks>
    /// <example>
    /// Registration in Startup.ConfigureServices:
    /// <code>
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     // Configure service user settings - required for daemon user fallback
    ///     services.Configure&lt;ServiceUserSettings&gt;(Configuration.GetSection("ServiceUser"));
    ///     
    ///     // Add hybrid user accessor that falls back to daemon user when needed
    ///     services.AddHybridUserAccessor();
    ///     
    ///     // Other service registrations...
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddHybridUserAccessor(this IServiceCollection services) {
      // Register the HTTP context accessor
      services.AddHttpContextAccessor();

      // Register the required user accessors internally
      services.AddScoped<HttpContextUserAccessor>();
      services.AddSingleton<DaemonUserAccessor>();

      // Register the hybrid user accessor as the IUserAccessor implementation
      services.AddScoped<IUserAccessor, HybridUserAccessor>();

      return services;
    }
  }
}
