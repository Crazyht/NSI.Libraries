namespace NSI.Core.Identity;
/// <summary>
/// Configuration settings for a service or system user account.
/// </summary>
/// <remarks>
/// <para>
/// This class defines the configuration properties used by <see cref="DaemonUserAccessor"/> 
/// to create a non-interactive system account that can perform automated operations.
/// </para>
/// <para>
/// Register this class in the application configuration system to provide identity
/// information for background services, automated processes, or system operations.
/// </para>
/// </remarks>
/// <example>
/// Configuration in appsettings.json:
/// <code>
/// {
///   "ServiceUser": {
///     "Id": "11111111-1111-1111-1111-111111111111",
///     "Username": "SystemService",
///     "DisplayName": "System Service"
///   }
/// }
/// </code>
/// 
/// Registration in service collection:
/// <code>
/// services.Configure&lt;ServiceUserSettings&gt;(
///     configuration.GetSection("ServiceUser"));
/// services.AddSingleton&lt;IUserAccessor, DaemonUserAccessor&gt;();
/// </code>
/// </example>
public class ServiceUserSettings {
  /// <summary>
  /// Gets or sets the unique identifier for the service user.
  /// </summary>
  /// <remarks>
  /// This ID is used to create a <see cref="Domains.UserId"/> for attribution of 
  /// automated operations in audit logs and entity tracking.
  /// </remarks>
  public Guid Id { get; set; }

  /// <summary>
  /// Gets or sets the login name or account identifier for the service user.
  /// </summary>
  /// <remarks>
  /// If not specified, defaults to "Daemon" when used by <see cref="DaemonUserAccessor"/>.
  /// </remarks>
  public string? Username { get; set; }

  /// <summary>
  /// Gets or sets the human-readable name for the service user.
  /// </summary>
  /// <remarks>
  /// This name is used for display purposes in user interfaces when showing who performed an operation.
  /// If not specified, defaults to "Daemon" when used by <see cref="DaemonUserAccessor"/>.
  /// </remarks>
  public string? DisplayName { get; set; }
}
