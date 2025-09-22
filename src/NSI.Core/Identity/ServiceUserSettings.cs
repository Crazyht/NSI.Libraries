namespace NSI.Core.Identity;

/// <summary>
/// Configuration settings for a service or system user account used in non-interactive operations.
/// </summary>
/// <remarks>
/// <para>
/// This class defines the configuration properties used by <see cref="DaemonUserAccessor"/> 
/// to create a non-interactive system account that can perform automated operations.
/// These settings enable background services, scheduled jobs, and automated processes
/// to have a consistent user identity for authentication, authorization, and audit tracking.
/// </para>
/// <para>
/// Configuration binding and usage:
/// <list type="bullet">
///   <item><description>Typically bound from configuration sections like "ServiceUser"</description></item>
///   <item><description>Used by dependency injection to configure <see cref="DaemonUserAccessor"/></description></item>
///   <item><description>Supports default values for username and display name when not specified</description></item>
///   <item><description>Integrates with ASP.NET Core configuration system and Options pattern</description></item>
/// </list>
/// </para>
/// <para>
/// Security considerations: The service user ID should be unique per environment 
/// and consistent across application restarts to maintain audit trail integrity.
/// Use different IDs for development, staging, and production environments.
/// </para>
/// <para>
/// Thread Safety: This class is designed to be configured once during application startup
/// and read-only thereafter, making it inherently thread-safe when used as intended
/// with the Options pattern.
/// </para>
/// </remarks>
/// <example>
/// Configuration in appsettings.json:
/// <code>
/// {
///   "ServiceUser": {
///     "Id": "11111111-1111-1111-1111-111111111111",
///     "Username": "SystemService",
///     "DisplayName": "System Service Account"
///   }
/// }
/// </code>
/// 
/// Registration in service collection:
/// <code>
/// // Configure from appsettings section
/// services.Configure&lt;ServiceUserSettings&gt;(
///   configuration.GetSection("ServiceUser"));
/// 
/// // Register daemon user accessor
/// services.AddSingleton&lt;IUserAccessor, DaemonUserAccessor&gt;();
/// </code>
/// 
/// Advanced configuration with validation:
/// <code>
/// services.Configure&lt;ServiceUserSettings&gt;(configuration.GetSection("ServiceUser"))
///   .PostConfigure&lt;ServiceUserSettings&gt;(settings => {
///     if (settings.Id == Guid.Empty) {
///       throw new InvalidOperationException("ServiceUser.Id cannot be empty");
///     }
///   });
/// </code>
/// 
/// Usage in background services:
/// <code>
/// public class DataProcessingService : BackgroundService {
///   private readonly IUserAccessor _userAccessor;
///   
///   public DataProcessingService(IUserAccessor userAccessor) {
///     _userAccessor = userAccessor;
///   }
///   
///   protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
///     var user = await _userAccessor.GetCurrentUserInfoAsync();
///     // user.Id will be the configured service user ID
///     // All operations will be attributed to this system account
///   }
/// }
/// </code>
/// </example>
public sealed class ServiceUserSettings {
  /// <summary>
  /// Gets or sets the unique identifier for the service user account.
  /// </summary>
  /// <value>
  /// A <see cref="Guid"/> that uniquely identifies the service user across the system.
  /// Should be consistent across application restarts and deployments within the same environment.
  /// </value>
  /// <remarks>
  /// <para>
  /// This ID is used to create a <see cref="NSI.Domains.UserId"/> for attribution of 
  /// automated operations in audit logs, entity tracking, and security contexts.
  /// The ID enables tracing all system-generated activities back to a consistent identity.
  /// </para>
  /// <para>
  /// Best practices:
  /// <list type="bullet">
  ///   <item><description>Use different GUIDs for different environments (dev, staging, prod)</description></item>
  ///   <item><description>Keep the same GUID across deployments within an environment</description></item>
  ///   <item><description>Avoid <see cref="Guid.Empty"/> as it may cause validation errors</description></item>
  ///   <item><description>Document the GUID value in deployment documentation</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public Guid Id { get; set; }

  /// <summary>
  /// Gets or sets the login name or account identifier for the service user.
  /// </summary>
  /// <value>
  /// A string representing the service account username, or null to use the default value.
  /// When null, <see cref="DaemonUserAccessor"/> will default to "Daemon".
  /// </value>
  /// <remarks>
  /// <para>
  /// This username is used for display purposes in logs, audit trails, and administrative
  /// interfaces. It should be descriptive enough to identify the purpose of the service
  /// account while remaining concise.
  /// </para>
  /// <para>
  /// Naming conventions:
  /// <list type="bullet">
  ///   <item><description>Use descriptive names like "DataProcessingService" or "ReportGenerator"</description></item>
  ///   <item><description>Avoid generic names like "Service" or "System" in multi-service applications</description></item>
  ///   <item><description>Consider including environment suffix for clarity (e.g., "DataService-Prod")</description></item>
  ///   <item><description>Keep names under 50 characters for database and UI compatibility</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Default behavior: If not specified, <see cref="DaemonUserAccessor"/> will use "Daemon" 
  /// as the username, which may be sufficient for single-service applications.
  /// </para>
  /// </remarks>
  /// <example>
  /// Examples of good usernames:
  /// <code>
  /// // Specific service names
  /// "OrderProcessingService"
  /// "EmailNotificationService" 
  /// "DataSyncService"
  /// 
  /// // With environment context
  /// "ReportService-Production"
  /// "BackupService-Staging"
  /// </code>
  /// </example>
  public string? Username { get; set; }

  /// <summary>
  /// Gets or sets the human-readable display name for the service user account.
  /// </summary>
  /// <value>
  /// A string representing the friendly display name for the service account, 
  /// or null to use the default value. When null, <see cref="DaemonUserAccessor"/> 
  /// will default to "Daemon".
  /// </value>
  /// <remarks>
  /// <para>
  /// This display name is used for presentation purposes in user interfaces, reports,
  /// and administrative tools when showing who performed an operation. It should be
  /// more descriptive and user-friendly than the <see cref="Username"/>.
  /// </para>
  /// <para>
  /// Display name guidelines:
  /// <list type="bullet">
  ///   <item><description>Use proper capitalization and spacing for readability</description></item>
  ///   <item><description>Include descriptive context about the service's purpose</description></item>
  ///   <item><description>Keep length reasonable for UI display (typically under 100 characters)</description></item>
  ///   <item><description>Consider including service version or environment for clarity</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Default behavior: If not specified, <see cref="DaemonUserAccessor"/> will use "Daemon" 
  /// as the display name. For production applications, providing a meaningful display name
  /// improves auditability and user experience.
  /// </para>
  /// </remarks>
  /// <example>
  /// Examples of effective display names:
  /// <code>
  /// // Descriptive service names
  /// "Order Processing Service"
  /// "Email Notification System"
  /// "Automated Data Synchronization Service"
  /// 
  /// // With environment and version context  
  /// "Report Generation Service (Production v2.1)"
  /// "Background Job Processor (Staging Environment)"
  /// </code>
  /// </example>
  public string? DisplayName { get; set; }
}
