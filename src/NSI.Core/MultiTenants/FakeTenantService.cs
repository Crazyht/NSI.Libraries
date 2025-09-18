namespace NSI.Core.MultiTenants;

/// <summary>
/// A test-oriented implementation of <see cref="ITenantService"/> that always returns a predefined fake tenant identifier.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is designed specifically for testing, local development, and scenarios where
/// multi-tenancy functionality needs to be bypassed or mocked. Unlike a production tenant service
/// that would determine the current tenant from HTTP headers, claims, or other contextual information,
/// this service always returns <see cref="TenantId.FakeTenantId"/>.
/// </para>
/// <para>
/// Key characteristics and use cases:
/// <list type="bullet">
///   <item><description>Testing scenarios: Provides consistent, predictable tenant context for unit and integration tests</description></item>
///   <item><description>Local development: Simplifies development setup by eliminating tenant resolution complexity</description></item>
///   <item><description>Single-tenant deployments: Can be used when multi-tenancy is not required but the codebase supports it</description></item>
///   <item><description>Fallback behavior: Serves as a safe default when tenant resolution fails or is not configured</description></item>
/// </list>
/// </para>
/// <para>
/// Thread safety: This implementation is stateless and thread-safe, making it suitable for
/// registration as a singleton service in dependency injection containers.
/// </para>
/// <para>
/// Performance considerations: This service has minimal overhead since it returns a static
/// value without any computation, HTTP context access, or external dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration in dependency injection container
/// services.AddSingleton&lt;ITenantService, FakeTenantService&gt;();
/// 
/// // Usage in application code
/// public class UserService {
///   private readonly ITenantService _tenantService;
///   
///   public UserService(ITenantService tenantService) {
///     _tenantService = tenantService;
///   }
///   
///   public async Task&lt;User&gt; GetUserAsync(UserId userId) {
///     var tenantId = _tenantService.GetCurrentTenantId();
///     // tenantId will always be TenantId.FakeTenantId
///     
///     return await _userRepository.GetUserAsync(userId, tenantId);
///   }
/// }
/// 
/// // Test scenario usage
/// [Fact]
/// public void GetCurrentTenantId_ShouldReturnFakeTenantId() {
///   var service = new FakeTenantService();
///   
///   var result = service.GetCurrentTenantId();
///   
///   Assert.Equal(TenantId.FakeTenantId, result);
/// }
/// 
/// // Integration test with consistent tenant context
/// public class IntegrationTestBase {
///   protected readonly ITenantService TenantService = new FakeTenantService();
///   
///   [Fact]
///   public async Task DataAccess_WithFakeTenant_ShouldWorkCorrectly() {
///     var service = new MyService(TenantService);
///     
///     var result = await service.ProcessDataAsync();
///     
///     // All operations will use TenantId.FakeTenantId consistently
///     Assert.NotNull(result);
///   }
/// }
/// </code>
/// </example>
/// <seealso cref="ITenantService"/>
/// <seealso cref="TenantId"/>
/// <seealso cref="TenantId.FakeTenantId"/>
public sealed class FakeTenantService: ITenantService {
  
  /// <summary>
  /// Gets a predefined fake tenant identifier for testing and development scenarios.
  /// </summary>
  /// <returns>
  /// A <see cref="TenantId"/> representing a fake tenant context.
  /// This method always returns <see cref="TenantId.FakeTenantId"/>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method provides deterministic behavior by always returning the same fake tenant ID,
  /// making it ideal for scenarios where consistent tenant context is required without the
  /// complexity of actual tenant resolution.
  /// </para>
  /// <para>
  /// Unlike production implementations that might return null or <see cref="TenantId.Empty"/>
  /// when no tenant context is available, this method always returns a valid fake tenant ID
  /// to ensure predictable behavior in test scenarios.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var service = new FakeTenantService();
  /// var tenantId = service.GetCurrentTenantId();
  /// 
  /// // tenantId is always TenantId.FakeTenantId
  /// Assert.Equal(TenantId.FakeTenantId, tenantId);
  /// 
  /// // Can be used in data access operations
  /// var users = await userRepository.GetUsersForTenantAsync(tenantId);
  /// </code>
  /// </example>
  public TenantId GetCurrentTenantId() => TenantId.FakeTenantId;
}
