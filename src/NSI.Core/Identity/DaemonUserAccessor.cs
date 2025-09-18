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
/// as a singleton service in the dependency injection container.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Consistent system identity for non-interactive operations</description></item>
///   <item><description>Configuration-based or explicit initialization</description></item>
///   <item><description>Thread-safe singleton design</description></item>
///   <item><description>Automatic fallback to "Daemon" defaults when configuration is incomplete</description></item>
/// </list>
/// </para>
/// <para>
/// Thread Safety: This class is thread-safe as the <see cref="_UserInfo"/> field is
/// initialized once during construction and never modified thereafter.
/// </para>
/// </remarks>
/// <example>
/// Registration in service collection with configuration:
/// <code>
/// // Configure service user settings from configuration
/// services.Configure&lt;ServiceUserSettings&gt;(
///     configuration.GetSection("ServiceUser"));
///     
/// // Register as singleton for background services
/// services.AddSingleton&lt;IUserAccessor, DaemonUserAccessor&gt;();
/// </code>
/// 
/// Registration with explicit values:
/// <code>
/// // Direct registration for testing or when configuration is not available
/// services.AddSingleton&lt;IUserAccessor&gt;(serviceProvider => 
///     new DaemonUserAccessor(
///         new UserId(Guid.Parse("11111111-1111-1111-1111-111111111111")), 
///         "TestDaemon", 
///         "Test Daemon User"));
/// </code>
/// 
/// Usage in a background service:
/// <code>
/// public class BackgroundJob : BackgroundService {
///   private readonly IUserAccessor _userAccessor;
///   private readonly IDbContext _dbContext;
///   
///   public BackgroundJob(IUserAccessor userAccessor, IDbContext dbContext) {
///     _userAccessor = userAccessor;
///     _dbContext = dbContext;
///   }
///   
///   protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
///     var user = await _userAccessor.GetCurrentUserInfoAsync();
///     // All database operations will use the daemon user for auditing
///     // user.Id will be "UserId-11111111-1111-1111-1111-111111111111"
///     await _dbContext.SaveChangesAsync(stoppingToken);
///   }
/// }
/// </code>
/// </example>
public sealed class DaemonUserAccessor: IUserAccessor {
  private readonly UserInfo _UserInfo;

  /// <summary>
  /// Initializes a new instance of the <see cref="DaemonUserAccessor"/> class using configuration.
  /// </summary>
  /// <param name="options">The options containing daemon user configuration settings.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="options"/> or its <see cref="IOptions{TOptions}.Value"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// The constructor initializes a fixed <see cref="UserInfo"/> instance using the provided settings.
  /// If the <see cref="ServiceUserSettings.Username"/> or <see cref="ServiceUserSettings.DisplayName"/> 
  /// are not specified in the configuration, they default to "Daemon".
  /// </para>
  /// <para>
  /// The daemon user is automatically marked as a system user (IsSystemUser = true) and 
  /// has no assigned roles (empty roles collection).
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
  /// </example>
  public DaemonUserAccessor(IOptions<ServiceUserSettings> options) {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(options.Value);

    var settings = options.Value;
    _UserInfo = new UserInfo(
      new UserId(settings.Id),
      settings.Username ?? "Daemon",
      settings.DisplayName ?? "Daemon",
      true,
      []);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="DaemonUserAccessor"/> class with explicit user identity values.
  /// </summary>
  /// <param name="id">The unique identifier for the daemon user.</param>
  /// <param name="username">The username for the daemon user.</param>
  /// <param name="displayName">The display name for the daemon user.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="username"/> or <paramref name="displayName"/> is null or whitespace.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This constructor allows direct creation of a daemon user with explicit identity values,
  /// which is useful for testing scenarios or when configuration-based initialization is not appropriate.
  /// </para>
  /// <para>
  /// Unlike the configuration-based constructor, this constructor does not provide default values
  /// and requires all parameters to be non-null and non-empty. The daemon user is automatically 
  /// marked as a system user with no assigned roles.
  /// </para>
  /// <para>
  /// Performance Note: This constructor creates the <see cref="UserInfo"/> object immediately,
  /// avoiding any runtime overhead during user access operations.
  /// </para>
  /// </remarks>
  /// <example>
  /// Creating a daemon user accessor for testing:
  /// <code>
  /// var testDaemon = new DaemonUserAccessor(
  ///   new UserId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
  ///   "TestDaemon",
  ///   "Test Daemon User");
  ///   
  /// var userInfo = await testDaemon.GetCurrentUserInfoAsync();
  /// Assert.Equal("TestDaemon", userInfo.Username);
  /// Assert.True(userInfo.IsSystemUser);
  /// </code>
  /// </example>
  public DaemonUserAccessor(UserId id, string username, string displayName) {
    ArgumentNullException.ThrowIfNull(id);
    ArgumentException.ThrowIfNullOrWhiteSpace(username);
    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

    _UserInfo = new UserInfo(
      id,
      username,
      displayName,
      true,
      []);
  }

  /// <summary>
  /// Gets information about the daemon user asynchronously.
  /// </summary>
  /// <returns>
  /// A <see cref="ValueTask{TResult}"/> that completes synchronously with the daemon <see cref="UserInfo"/>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This implementation always returns the same user information regardless of the execution context,
  /// providing a consistent identity for system operations. The returned task completes synchronously
  /// since no actual user resolution or I/O operations are performed.
  /// </para>
  /// <para>
  /// The returned <see cref="UserInfo"/> contains:
  /// <list type="bullet">
  ///   <item><description>A unique <see cref="UserId"/> for audit tracking</description></item>
  ///   <item><description>Username and display name for identification</description></item>
  ///   <item><description>IsSystemUser set to true to indicate non-human user</description></item>
  ///   <item><description>Empty roles collection as system users typically bypass role-based authorization</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Performance: This method returns a cached <see cref="UserInfo"/> instance with no allocations
  /// or computations, making it extremely fast for repeated calls.
  /// </para>
  /// </remarks>
  /// <example>
  /// Using the daemon user in an audit service:
  /// <code>
  /// public async Task RecordSystemAction(string actionType, string details) {
  ///   var user = await _userAccessor.GetCurrentUserInfoAsync();
  ///   
  ///   var auditEntry = new AuditLogEntry {
  ///     UserId = user.Id,
  ///     Username = user.Username,  // e.g., "SystemService"
  ///     ActionType = actionType,
  ///     Details = details,
  ///     Timestamp = DateTime.UtcNow,
  ///     IsSystemGenerated = user.IsSystemUser  // Will be true
  ///   };
  ///   
  ///   await _auditRepository.AddAsync(auditEntry);
  /// }
  /// </code>
  /// </example>
  public ValueTask<UserInfo> GetCurrentUserInfoAsync() => ValueTask.FromResult(_UserInfo);
}
