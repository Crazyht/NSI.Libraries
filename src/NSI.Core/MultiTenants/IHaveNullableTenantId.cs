namespace NSI.Core.MultiTenants;

/// <summary>
/// Defines an entity that can optionally belong to a specific tenant in a multi-tenant system with nullable tenant identification.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that can exist in both tenant-scoped and global contexts.
/// This is useful for shared reference data, system-wide configurations, or entities that need to
/// be accessible across multiple tenants.
/// </para>
/// <para>
/// Key characteristics and use cases:
/// <list type="bullet">
///   <item><description>Optional tenant scope: Entities can belong to a specific tenant or be globally accessible</description></item>
///   <item><description>Shared data support: Enables entities that are visible across tenant boundaries</description></item>
///   <item><description>System configurations: Perfect for application settings that can be tenant-specific or global</description></item>
///   <item><description>Reference data: Suitable for lookup tables that may be shared or tenant-specific</description></item>
/// </list>
/// </para>
/// <para>
/// When <see cref="TenantId"/> is null, the entity is considered globally accessible.
/// When it has a value, the entity is scoped to that specific tenant.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Shared configuration entity
/// public class ApplicationSetting: IHaveNullableTenantId {
///   public SettingId Id { get; init; } = SettingId.New();
///   public TenantId? TenantId { get; init; } // null = global, value = tenant-specific
///   public string Key { get; init; } = string.Empty;
///   public string Value { get; init; } = string.Empty;
/// }
/// 
/// // Repository handling both global and tenant-scoped settings
/// public class SettingRepository {
///   private readonly ITenantService _tenantService;
///   private readonly DbContext _context;
///   
///   public async Task&lt;ApplicationSetting?&gt; GetSettingAsync(string key) {
///     var currentTenant = _tenantService.GetCurrentTenantId();
///     
///     return await _context.Settings
///       .Where(s => s.Key == key &amp;&amp; 
///                  (s.TenantId == currentTenant || s.TenantId == null))
///       .OrderBy(s => s.TenantId == null ? 1 : 0) // Tenant-specific first
///       .FirstOrDefaultAsync();
///   }
/// }
/// 
/// // Creating global vs tenant-specific settings
/// var globalSetting = new ApplicationSetting {
///   Key = "MaxUploadSize",
///   Value = "10MB",
///   TenantId = null // Global setting
/// };
/// 
/// var tenantSetting = new ApplicationSetting {
///   Key = "MaxUploadSize", 
///   Value = "50MB",
///   TenantId = tenantService.GetCurrentTenantId() // Tenant-specific override
/// };
/// </code>
/// </example>
/// <seealso cref="IHaveTenantId"/>
/// <seealso cref="IHaveInheritedTenantId"/>
/// <seealso cref="TenantId"/>
/// <seealso cref="ITenantService"/>
public interface IHaveNullableTenantId {

  /// <summary>
  /// Gets the tenant identifier this entity belongs to, or null if the entity is globally accessible.
  /// </summary>
  /// <value>
  /// A strongly-typed tenant identifier, or null for globally accessible entities.
  /// When null, the entity is visible across all tenant boundaries.
  /// </value>
  /// <remarks>
  /// <para>
  /// This property uses the init-only setter pattern to ensure immutability after construction.
  /// A null value indicates the entity is globally accessible, while a non-null value
  /// scopes the entity to the specified tenant.
  /// </para>
  /// <para>
  /// Repository implementations should handle both cases appropriately, typically
  /// by including null tenant IDs in queries alongside the current tenant ID.
  /// </para>
  /// </remarks>
  public TenantId? TenantId { get; init; }
}
