using System.Globalization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Core.MultiTenants;

/// <summary>
/// Defines an entity that belongs to a specific tenant in a multi-tenant system.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that should be scoped to a specific tenant.
/// This enables tenant-based filtering and data isolation in multi-tenant applications.
/// </para>
/// </remarks>
public interface IHaveTenantId {
  /// <summary>
  /// Gets the tenant identifier this entity belongs to.
  /// </summary>
  /// <value>A strongly-typed tenant identifier.</value>
  public TenantId TenantId { get; init; }
}

/// <summary>
/// Defines an entity that belongs to a specific tenant in a multi-tenant system.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that should be scoped to a specific tenant.
/// This enables tenant-based filtering and data isolation in multi-tenant applications.
/// </para>
/// </remarks>
public interface IHaveNullableTenantId {
  /// <summary>
  /// Gets the tenant identifier this entity belongs to.
  /// </summary>
  /// <value>A strongly-typed tenant identifier.</value>
  public TenantId? TenantId { get; init; }
}

/// <summary>
/// Strongly-typed identifier for tenant entities.
/// </summary>
/// <param name="Value">The underlying GUID value of the tenant identifier.</param>
/// <remarks>
/// <para>
/// This identifier uses the strongly-typed ID pattern to provide type safety
/// and prevent accidental mixing of different ID types.
/// </para>
/// </remarks>
public sealed record TenantId(Guid Value): StronglyTypedId<TenantId, Guid>(Value) {
  /// <summary>
  /// Overrides the default record-generated string representation to use a prefixed format.
  /// </summary>
  /// <returns>A string representation in the format "TenantId-{Value}".</returns>
  public override string ToString() => ToSerializedString(this);

  /// <summary>
  /// Gets an empty tenant ID representing no specific tenant.
  /// </summary>
  /// <value>A <see cref="TenantId"/> initialized with <see cref="Guid.Empty"/>.</value>
  public static TenantId Empty => new(Guid.Empty);

  /// <summary>
  /// Gets an tenant ID representing fake tenant.
  /// </summary>
  public static TenantId FakeTenantId => new(Guid.Parse("00000000-0000-0000-0000-000000000001", CultureInfo.InvariantCulture));
}

/// <summary>
/// Marker interface used to disable automatic query filtering on child entities of filtered parent entities.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that do not have a direct <c>TenantId</c> property,
/// and whose tenant context is inherited from a parent entity. By marking an entity with this interface,
/// automatic multi-tenant query filters will not be applied to it, even if its parent entity is filtered.
/// </para>
/// <para>
/// This is useful for aggregate children or related entities in multi-tenant systems where
/// tenant scoping and filtering should only be enforced at the parent/root level.
/// </para>
/// </remarks>
public interface IHaveInheritedTenantId { }
