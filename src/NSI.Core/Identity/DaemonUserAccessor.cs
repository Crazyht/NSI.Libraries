using Microsoft.Extensions.Options;
using NSI.Domains;

namespace NSI.Core.Identity;
/// <summary>
/// Implementation of <see cref="IUserAccessor"/> that provides a fixed system user identity
/// for non-interactive operations such as background services and scheduled tasks.
/// </summary>
/// <remarks>
/// <para>
/// This class creates a static user identity from configuration settings that represents
/// a system or daemon account. This enables background processes, scheduled jobs, and other
/// non-interactive operations to have a consistent user identity for authentication and auditing.
/// </para>
/// <para>
/// The daemon user's information is set once during initialization and remains constant
/// throughout the lifetime of the application. This implementation is designed to be registered
/// as a singleton service.
/// </para>
/// <para>
/// Common use cases include:
/// <list type="bullet">
///   <item><description>Background data processing jobs</description></item>
///   <item><description>Scheduled maintenance tasks</description></item>
///   <item><description>System-initiated database operations</description></item>
///   <item><description>Integration with external systems requiring a service account</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Registration in service collection:
/// <code>
/// // Configure service user settings from configuration
/// services.Configure&lt;ServiceUserSettings&gt;(
///     configuration.GetSection("ServiceUser"));
///     
/// // Register as singleton for background services
/// services.AddSingleton&lt;IUserAccessor, DaemonUserAccessor&gt;();
/// </code>
/// 
/// Usage in a background service:
/// <code>
/// public class BackgroundJob : BackgroundService
/// {
///     private readonly IUserAccessor _userAccessor;
///     private readonly IDbContext _dbContext;
///     
///     public BackgroundJob(IUserAccessor userAccessor, IDbContext dbContext)
///     {
///         _userAccessor = userAccessor;
///         _dbContext = dbContext;
///     }
///     
///     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
///     {
///         // All database operations will use the daemon user for auditing
///         await _dbContext.SaveChangesAsync(stoppingToken);
///     }
/// }
/// </code>
/// </example>
public class DaemonUserAccessor: IUserAccessor {
  private readonly UserInfo _UserInfo;

  /// <summary>
  /// Initializes a new instance of the <see cref="DaemonUserAccessor"/> class.
  /// </summary>
  /// <param name="options">The options containing daemon user configuration settings.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or its value is null.</exception>
  /// <remarks>
  /// The constructor initializes a fixed <see cref="UserInfo"/> instance using the provided settings.
  /// If the username or display name are not specified in the configuration, they default to "Daemon".
  /// </remarks>
  public DaemonUserAccessor(IOptions<ServiceUserSettings> options) {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(options.Value);

    _UserInfo = new(
        new UserId(options.Value.Id),
        options.Value.Username ?? "Daemon",
        options.Value.DisplayName ?? "Daemon",
        true,
        []);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="DaemonUserAccessor"/> class with explicit user identity values.
  /// </summary>
  /// <param name="id">The unique identifier for the daemon user.</param>
  /// <param name="username">The username for the daemon user.</param>
  /// <param name="displayName">The display name for the daemon user.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="id"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown if <paramref name="username"/> or <paramref name="displayName"/> is null or whitespace.</exception>
  /// <remarks>
  /// <para>
  /// This constructor allows direct creation of a daemon user with explicit identity values,
  /// which is useful for testing scenarios or when configuration-based initialization is not appropriate.
  /// </para>
  /// <para>
  /// Unlike the configuration-based constructor, this constructor does not provide default values
  /// and requires all parameters to be non-null and non-empty.
  /// </para>
  /// </remarks>
  public DaemonUserAccessor(UserId id, string username, string displayName) {
    ArgumentNullException.ThrowIfNull(id);
    ArgumentException.ThrowIfNullOrWhiteSpace(username);
    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
    _UserInfo = new(id, username, displayName, true, []);
  }

  /// <summary>
  /// Gets information about the daemon user asynchronously.
  /// </summary>
  /// <returns>A <see cref="ValueTask{TResult}"/> containing the daemon <see cref="UserInfo"/>.</returns>
  /// <remarks>
  /// <para>
  /// This implementation always returns the same user information regardless of the execution context,
  /// providing a consistent identity for system operations. The returned task completes synchronously
  /// since no actual user resolution is performed.
  /// </para>
  /// <para>
  /// Although this method is implemented to match the asynchronous contract of <see cref="IUserAccessor.GetCurrentUserInfoAsync"/>,
  /// it does not perform any asynchronous operations.
  /// </para>
  /// </remarks>
  public ValueTask<UserInfo> GetCurrentUserInfoAsync() => ValueTask.FromResult(_UserInfo);
}
