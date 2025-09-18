namespace NSI.Core.Identity;

/// <summary>
/// Defines an abstraction for accessing information about the current user in various execution contexts.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a standard way to obtain user identity information in a context-independent manner.
/// By abstracting user access logic, application components can work with user information without being
/// tightly coupled to the specific authentication mechanism or user source.
/// </para>
/// <para>
/// The implementation of this interface varies based on execution context and application requirements:
/// <list type="bullet">
///   <item><description>In web applications, implementations extract user details from HTTP context, JWT tokens, or authentication cookies</description></item>
///   <item><description>In background services, implementations use <see cref="DaemonUserAccessor"/> to provide consistent system user credentials</description></item>
///   <item><description>In testing scenarios, implementations provide predictable mock user information for unit and integration tests</description></item>
///   <item><description>In desktop applications, implementations may use Windows authentication or local user profiles</description></item>
/// </list>
/// </para>
/// <para>
/// Key architectural benefits:
/// <list type="bullet">
///   <item><description>Dependency inversion - business logic depends on abstraction, not concrete user sources</description></item>
///   <item><description>Testability - easy to mock and provide predictable user data in tests</description></item>
///   <item><description>Flexibility - can switch authentication mechanisms without changing business code</description></item>
///   <item><description>Audit consistency - ensures all operations are attributed to identifiable users</description></item>
/// </list>
/// </para>
/// <para>
/// This interface is particularly important for audit tracking functionality, as it enables 
/// automatic attribution of database operations, business actions, and system events to specific users.
/// The returned <see cref="UserInfo"/> provides structured identity data that can be used for
/// authentication, authorization, personalization, and compliance requirements.
/// </para>
/// <para>
/// Thread Safety: Implementations of this interface should be thread-safe, as user information
/// may be accessed concurrently in multi-threaded scenarios such as web applications or background services.
/// </para>
/// </remarks>
/// <example>
/// Registering implementations in dependency injection container:
/// <code>
/// // For web applications with JWT authentication
/// services.AddScoped&lt;IUserAccessor, JwtUserAccessor&gt;();
/// 
/// // For background services using system account
/// services.Configure&lt;ServiceUserSettings&gt;(config => {
///   config.Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
///   config.Username = "SystemService";
///   config.DisplayName = "System Service Account";
/// });
/// services.AddSingleton&lt;IUserAccessor, DaemonUserAccessor&gt;();
/// 
/// // For testing scenarios with mock user
/// services.AddSingleton&lt;IUserAccessor&gt;(_ => 
///   new DaemonUserAccessor(
///     new UserId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
///     "TestUser",
///     "Test User Account"));
/// </code>
/// 
/// Using in service classes for audit tracking:
/// <code>
/// public class OrderService {
///   private readonly IUserAccessor _userAccessor;
///   private readonly IOrderRepository _orderRepository;
///   private readonly ILogger&lt;OrderService&gt; _logger;
///   
///   public OrderService(
///     IUserAccessor userAccessor, 
///     IOrderRepository orderRepository,
///     ILogger&lt;OrderService&gt; logger) {
///     _userAccessor = userAccessor;
///     _orderRepository = orderRepository;
///     _logger = logger;
///   }
///   
///   public async Task&lt;Result&lt;Order&gt;&gt; CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default) {
///     var user = await _userAccessor.GetCurrentUserInfoAsync();
///     
///     _logger.LogInformation("Creating order for user {UserId} ({Email})", 
///       user.Id, user.Email);
///     
///     var order = new Order {
///       CreatedBy = user.Id,
///       CreatedAt = DateTime.UtcNow,
///       // ... other properties
///     };
///     
///     return await _orderRepository.CreateAsync(order, cancellationToken);
///   }
/// }
/// </code>
/// 
/// Using in middleware for request context:
/// <code>
/// public class AuditMiddleware {
///   private readonly RequestDelegate _next;
///   private readonly IUserAccessor _userAccessor;
///   
///   public AuditMiddleware(RequestDelegate next, IUserAccessor userAccessor) {
///     _next = next;
///     _userAccessor = userAccessor;
///   }
///   
///   public async Task InvokeAsync(HttpContext context) {
///     var user = await _userAccessor.GetCurrentUserInfoAsync();
///     
///     // Add user information to logging scope
///     using (var scope = context.RequestServices.GetService&lt;ILogger&lt;AuditMiddleware&gt;&gt;()
///       ?.BeginScope(new Dictionary&lt;string, object&gt; {
///         ["UserId"] = user.Id.ToString(),
///         ["Email"] = user.Email,
///         ["IsActive"] = user.IsActive
///       })) {
///       
///       await _next(context);
///     }
///   }
/// }
/// </code>
/// </example>
public interface IUserAccessor {
  /// <summary>
  /// Gets information about the current user asynchronously.
  /// </summary>
  /// <returns>
  /// A <see cref="ValueTask{TResult}"/> that completes with the current <see cref="UserInfo"/>.
  /// The task may complete synchronously if user information is readily available in memory,
  /// or asynchronously if user details need to be retrieved from external sources.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method retrieves identity information for the user associated with the current execution context.
  /// The returned <see cref="UserInfo"/> contains essential identity data including:
  /// <list type="bullet">
  ///   <item><description><see cref="UserInfo.Id"/> - Unique identifier for database operations and audit trails</description></item>
  ///   <item><description><see cref="UserInfo.Email"/> - User's email address for login and communication purposes</description></item>
  ///   <item><description><see cref="UserInfo.FullName"/> - User's display name for UI presentation</description></item>
  ///   <item><description><see cref="UserInfo.IsActive"/> - Flag indicating whether the user account is active</description></item>
  ///   <item><description><see cref="UserInfo.Roles"/> - Collection of assigned roles for authorization decisions</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Implementation Considerations:
  /// <list type="bullet">
  ///   <item><description>Web applications should extract user information from HTTP context, claims, or session data</description></item>
  ///   <item><description>Background services should return consistent system user information via <see cref="DaemonUserAccessor"/></description></item>
  ///   <item><description>The method should be efficient for repeated calls, potentially caching user information per request scope</description></item>
  ///   <item><description>Implementations must handle unauthenticated scenarios appropriately (return guest user or throw exception)</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// This method is designed as asynchronous to support implementations that may need to retrieve
  /// user information from external sources such as identity providers, databases, or remote authentication services.
  /// However, implementations may complete synchronously using <see cref="ValueTask.FromResult{TResult}(TResult)"/>
  /// when user information is immediately available.
  /// </para>
  /// <para>
  /// Performance Note: Implementations should consider caching user information within the appropriate
  /// scope (request, session, or singleton) to avoid repeated lookups. The exact caching strategy
  /// depends on the authentication mechanism and application requirements.
  /// </para>
  /// </remarks>
  /// <example>
  /// Typical usage pattern in business services:
  /// <code>
  /// public async Task ProcessBusinessOperation() {
  ///   var user = await _userAccessor.GetCurrentUserInfoAsync();
  ///   
  ///   // Use user information for business logic
  ///   if (!user.IsActive &amp;&amp; !user.Roles.Contains("Manager")) {
  ///     throw new UnauthorizedAccessException("Manager role required");
  ///   }
  ///   
  ///   // Create audit record
  ///   var auditEntry = new AuditLogEntry {
  ///     UserId = user.Id,  // UserId-11111111-1111-1111-1111-111111111111
  ///     Email = user.Email,  // e.g., "john.doe@company.com"
  ///     Action = "ProcessBusinessOperation",
  ///     Timestamp = DateTime.UtcNow
  ///   };
  ///   
  ///   await _auditService.RecordAsync(auditEntry);
  /// }
  /// </code>
  /// 
  /// Error handling for unauthenticated scenarios:
  /// <code>
  /// public async Task HandleUnauthenticatedUser() {
  ///   try {
  ///     var user = await _userAccessor.GetCurrentUserInfoAsync();
  ///     // Process with authenticated user
  ///   }
  ///   catch (UnauthorizedAccessException) {
  ///     // Handle unauthenticated user scenario
  ///     // Return anonymous/guest user or redirect to login
  ///   }
  /// }
  /// </code>
  /// </example>
  /// <exception cref="UnauthorizedAccessException">
  /// Thrown when the user is not authenticated and unauthenticated access is not allowed.
  /// </exception>
  public ValueTask<UserInfo> GetCurrentUserInfoAsync();
}
