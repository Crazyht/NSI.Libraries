namespace NSI.Core.MultiTenants;

/// <summary>
/// Defines a service for retrieving the current tenant identifier in a multi-tenant system.
/// </summary>
/// <remarks>
/// <para>
/// This service is responsible for determining the active tenant context for the current operation.
/// It's typically used in data access operations to implement tenant-specific data isolation.
/// </para>
/// <para>
/// Implementations should extract tenant information from the current execution context,
/// such as HTTP request headers, claims, ambient context, or configuration.
/// </para>
/// </remarks>
public interface ITenantService {
  /// <summary>
  /// Gets the identifier of the current tenant.
  /// </summary>
  /// <returns>
  /// A <see cref="TenantId"/> representing the current tenant context.
  /// If no tenant context is available, implementations should return <see cref="TenantId.Empty"/>.
  /// </returns>
  public TenantId? GetCurrentTenantId();
}
