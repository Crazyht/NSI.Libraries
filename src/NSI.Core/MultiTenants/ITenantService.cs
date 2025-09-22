namespace NSI.Core.MultiTenants;

/// <summary>
/// Defines a service for retrieving the current tenant identifier in a multi-tenant system with comprehensive context resolution.
/// </summary>
/// <remarks>
/// <para>
/// This service is responsible for determining the active tenant context for the current operation
/// by extracting tenant information from various sources in the execution environment. It serves as
/// the central abstraction for tenant resolution across the entire application stack.
/// </para>
/// <para>
/// Key responsibilities and capabilities:
/// <list type="bullet">
///   <item><description>Tenant context resolution: Extracts tenant information from HTTP headers, JWT claims, request context, or ambient data</description></item>
///   <item><description>Data isolation support: Enables automatic tenant-scoped data filtering in repositories and ORM configurations</description></item>
///   <item><description>Security enforcement: Ensures tenant boundaries are maintained throughout request processing</description></item>
///   <item><description>Testing and development support: Provides consistent tenant context for unit tests and development scenarios</description></item>
/// </list>
/// </para>
/// <para>
/// Common implementation patterns and strategies:
/// <list type="bullet">
///   <item><description>HTTP-based resolution: Extract tenant ID from request headers, subdomain, or route parameters</description></item>
///   <item><description>Claims-based resolution: Retrieve tenant information from JWT tokens or authentication claims</description></item>
///   <item><description>Ambient context: Use AsyncLocal or similar mechanisms for thread-safe tenant context propagation</description></item>
///   <item><description>Configuration-based: Use application settings for single-tenant deployments</description></item>
///   <item><description>Fake/mock implementations: Provide deterministic tenant context for testing scenarios</description></item>
/// </list>
/// </para>
/// <para>
/// Thread safety considerations: Implementations should be thread-safe and support concurrent access.
/// When using ambient context patterns, ensure proper isolation between concurrent operations.
/// </para>
/// <para>
/// Performance considerations: This service may be called frequently during request processing,
/// so implementations should cache resolved tenant information when appropriate and avoid
/// expensive operations on each call.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // HTTP header-based implementation
/// public class HeaderTenantService: ITenantService {
///   private readonly IHttpContextAccessor _httpContextAccessor;
///   
///   public HeaderTenantService(IHttpContextAccessor httpContextAccessor) {
///     _httpContextAccessor = httpContextAccessor;
///   }
///   
///   public TenantId? GetCurrentTenantId() {
///     var context = _httpContextAccessor.HttpContext;
///     if (context?.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) == true) {
///       return TenantId.TryParse(tenantHeader.ToString(), null, out var tenantId) 
///         ? tenantId 
///         : null;
///     }
///     return null;
///   }
/// }
/// 
/// // Claims-based implementation
/// public class ClaimsTenantService: ITenantService {
///   private readonly IHttpContextAccessor _httpContextAccessor;
///   
///   public ClaimsTenantService(IHttpContextAccessor httpContextAccessor) {
///     _httpContextAccessor = httpContextAccessor;
///   }
///   
///   public TenantId? GetCurrentTenantId() {
///     var user = _httpContextAccessor.HttpContext?.User;
///     var tenantClaim = user?.FindFirst("tenant_id")?.Value;
///     
///     return !string.IsNullOrEmpty(tenantClaim) &amp;&amp; 
///            TenantId.TryParse(tenantClaim, null, out var tenantId)
///       ? tenantId
///       : null;
///   }
/// }
/// 
/// // Usage in repository with automatic filtering
/// public class UserRepository {
///   private readonly ITenantService _tenantService;
///   private readonly DbContext _context;
///   
///   public UserRepository(ITenantService tenantService, DbContext context) {
///     _tenantService = tenantService;
///     _context = context;
///   }
///   
///   public async Task&lt;List&lt;User&gt;&gt; GetUsersAsync() {
///     var currentTenant = _tenantService.GetCurrentTenantId();
///     if (currentTenant == null) {
///       throw new InvalidOperationException("No tenant context available");
///     }
///     
///     return await _context.Users
///       .Where(u => u.TenantId == currentTenant)
///       .ToListAsync();
///   }
/// }
/// 
/// // Dependency injection registration
/// services.AddScoped&lt;ITenantService, HeaderTenantService&gt;();
/// services.AddHttpContextAccessor(); // Required for HTTP-based implementations
/// 
/// // Testing with fake service
/// services.AddSingleton&lt;ITenantService, FakeTenantService&gt;();
/// </code>
/// </example>
/// <seealso cref="TenantId"/>
/// <seealso cref="FakeTenantService"/>
/// <seealso cref="IHaveTenantId"/>
/// <seealso cref="IHaveNullableTenantId"/>
/// <seealso cref="IHaveInheritedTenantId"/>
public interface ITenantService {

  /// <summary>
  /// Gets the identifier of the current tenant from the active execution context.
  /// </summary>
  /// <returns>
  /// A <see cref="TenantId"/> representing the current tenant context if available; 
  /// otherwise, <see langword="null"/> if no tenant context can be determined.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method should resolve the tenant context from the current execution environment.
  /// The specific mechanism depends on the implementation strategy and application architecture.
  /// </para>
  /// <para>
  /// Return value semantics and behavior:
  /// <list type="bullet">
  ///   <item><description>Non-null result: A valid tenant context was successfully resolved from the execution environment</description></item>
  ///   <item><description>Null result: No tenant context is available, which may be valid for certain operations or indicate an authentication issue</description></item>
  ///   <item><description>Consistent behavior: Multiple calls within the same execution context should return the same result</description></item>
  ///   <item><description>Error handling: Invalid or malformed tenant data should result in null rather than throwing exceptions</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Implementation guidelines and considerations:
  /// <list type="bullet">
  ///   <item><description>Performance: Cache resolved tenant information within the current request/operation scope when possible</description></item>
  ///   <item><description>Thread safety: Ensure implementation is safe for concurrent access in multi-threaded environments</description></item>
  ///   <item><description>Error resilience: Handle missing or invalid tenant data gracefully by returning null</description></item>
  ///   <item><description>Security: Validate tenant data to prevent injection attacks or unauthorized access</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Common data sources for tenant resolution include HTTP request headers (X-Tenant-Id),
  /// JWT claims (tenant_id), authentication principal claims, request subdomain, route parameters,
  /// or ambient context set by middleware or filters.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic usage in service layer
  /// public class OrderService {
  ///   private readonly ITenantService _tenantService;
  ///   private readonly IOrderRepository _orderRepository;
  ///   
  ///   public async Task&lt;Order&gt; CreateOrderAsync(CreateOrderRequest request) {
  ///     var tenantId = _tenantService.GetCurrentTenantId();
  ///     if (tenantId == null) {
  ///       throw new UnauthorizedAccessException("No tenant context available");
  ///     }
  ///     
  ///     var order = new Order {
  ///       TenantId = tenantId,
  ///       // ... other properties
  ///     };
  ///     
  ///     return await _orderRepository.CreateAsync(order);
  ///   }
  /// }
  /// 
  /// // Usage with null handling
  /// public class ReportService {
  ///   private readonly ITenantService _tenantService;
  ///   
  ///   public async Task&lt;Report&gt; GenerateReportAsync() {
  ///     var tenantId = _tenantService.GetCurrentTenantId();
  ///     
  ///     // Handle both tenant-scoped and global reports
  ///     return tenantId != null
  ///       ? await GenerateTenantReportAsync(tenantId)
  ///       : await GenerateGlobalReportAsync();
  ///   }
  /// }
  /// 
  /// // ASP.NET Core middleware integration
  /// public class TenantMiddleware {
  ///   public async Task InvokeAsync(HttpContext context, ITenantService tenantService) {
  ///     var tenantId = tenantService.GetCurrentTenantId();
  ///     
  ///     // Add tenant information to logging scope
  ///     using var scope = context.RequestServices
  ///       .GetService&lt;ILogger&lt;TenantMiddleware&gt;&gt;()
  ///       ?.BeginScope(new { TenantId = tenantId?.ToString() ?? "None" });
  ///     
  ///     // Continue pipeline
  ///     await _next(context);
  ///   }
  /// }
  /// 
  /// // Entity Framework query filter integration
  /// protected override void OnModelCreating(ModelBuilder modelBuilder) {
  ///   var tenantService = serviceProvider.GetService&lt;ITenantService&gt;();
  ///   
  ///   modelBuilder.Entity&lt;Order&gt;().HasQueryFilter(o => 
  ///     o.TenantId == tenantService.GetCurrentTenantId());
  /// }
  /// </code>
  /// </example>
  /// <seealso cref="TenantId"/>
  /// <seealso cref="TenantId.FakeTenantId"/>
  /// <seealso cref="FakeTenantService"/>
  public TenantId? GetCurrentTenantId();
}
