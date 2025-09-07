namespace NSI.Core.MultiTenants;

/// <summary>
/// A non-functional implementation of <see cref="ITenantService"/> that always returns an empty tenant.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is typically used for testing, local development, or scenarios where
/// multi-tenancy is not required. It always returns <see cref="TenantId.Empty"/>.
/// </para>
/// </remarks>
public class FakeTenantService: ITenantService {
  /// <summary>
  /// Gets an empty tenant ID, representing no specific tenant.
  /// </summary>
  /// <returns>An fake <see cref="TenantId"/> value.</returns>
  public TenantId GetCurrentTenantId() => TenantId.FakeTenantId;
}
