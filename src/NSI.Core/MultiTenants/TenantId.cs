using System.Globalization;

using NSI.Domains.StrongIdentifier;

namespace NSI.Core.MultiTenants;

/// <summary>
/// Strongly-typed identifier for tenant entities in multi-tenant systems.
/// </summary>
/// <param name="Value">The underlying GUID value of the tenant identifier.</param>
/// <remarks>
/// <para>
/// This identifier uses the strongly-typed ID pattern to provide type safety
/// and prevent accidental mixing of different ID types. It ensures that tenant IDs
/// cannot be confused with other GUID-based identifiers in the system.
/// </para>
/// <para>
/// Key features and benefits:
/// <list type="bullet">
///   <item><description>Type safety: Prevents mixing tenant IDs with other GUID-based IDs</description></item>
///   <item><description>Serialization support: Works seamlessly with JSON and Entity Framework</description></item>
///   <item><description>Debug-friendly: ToString() provides clear identification with type prefix</description></item>
///   <item><description>Immutability: Record type ensures the ID cannot be modified after creation</description></item>
///   <item><description>Validation: Inherits parsing and validation from base strongly-typed ID infrastructure</description></item>
/// </list>
/// </para>
/// <para>
/// The identifier supports special values for testing and system operations via
/// <see cref="Empty"/> and <see cref="FakeTenantId"/> properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating tenant IDs
/// var tenantId = new TenantId(Guid.NewGuid());
/// var emptyTenant = TenantId.Empty;
/// var fakeTenant = TenantId.FakeTenantId;
/// 
/// // Using in domain entities
/// public class Order: IHaveTenantId {
///   public OrderId Id { get; init; } = OrderId.New();
///   public TenantId TenantId { get; init; }
///   public decimal Amount { get; init; }
/// }
/// 
/// // Repository filtering by tenant
/// public async Task&lt;List&lt;Order&gt;&gt; GetOrdersAsync(TenantId tenantId) {
///   return await _context.Orders
///     .Where(o => o.TenantId == tenantId)
///     .ToListAsync();
/// }
/// 
/// // String representation and parsing
/// var tenantId = new TenantId(Guid.NewGuid());
/// var serialized = tenantId.ToString(); // "TenantId-{guid-value}"
/// var parsed = TenantId.TryParse(serialized, null, out var result);
/// </code>
/// </example>
/// <seealso cref="StronglyTypedId{TId, TUnderlying}"/>
/// <seealso cref="IHaveTenantId"/>
/// <seealso cref="IHaveNullableTenantId"/>
public sealed record TenantId(Guid Value): StronglyTypedId<TenantId, Guid>(Value) {

  /// <summary>
  /// Returns a string representation of this tenant ID using the standardized strongly-typed ID format.
  /// </summary>
  /// <returns>
  /// A string representation in the format "TenantId-{Value}" where Value is the GUID representation.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This override ensures consistent string representation across the application,
  /// making debugging and logging more effective by clearly identifying the type of ID.
  /// </para>
  /// <para>
  /// The format matches the pattern used by other strongly-typed IDs in the system,
  /// enabling consistent parsing and serialization behavior.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var tenantId = new TenantId(Guid.Parse("12345678-1234-5678-9012-123456789012"));
  /// var stringRepresentation = tenantId.ToString();
  /// // Result: "TenantId-12345678-1234-5678-9012-123456789012"
  /// 
  /// // Can be parsed back
  /// var success = TenantId.TryParse(stringRepresentation, null, out var parsedId);
  /// Assert.True(success);
  /// Assert.Equal(tenantId, parsedId);
  /// </code>
  /// </example>
  public override string ToString() => ToSerializedString(this);

  /// <summary>
  /// Gets an empty tenant ID representing no specific tenant or uninitialized tenant context.
  /// </summary>
  /// <value>A <see cref="TenantId"/> initialized with <see cref="Guid.Empty"/>.</value>
  /// <remarks>
  /// <para>
  /// This property provides a well-known empty value that can be used for comparisons,
  /// default values, or scenarios where no tenant context is available.
  /// </para>
  /// <para>
  /// Use this value when you need to represent the absence of a tenant ID in a non-nullable context,
  /// though consider using <see cref="IHaveNullableTenantId"/> if null tenant IDs are semantically meaningful.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Checking for empty tenant
  /// public bool IsValidTenant(TenantId tenantId) {
  ///   return tenantId != TenantId.Empty;
  /// }
  /// 
  /// // Default initialization
  /// public class SomeEntity: IHaveTenantId {
  ///   public TenantId TenantId { get; init; } = TenantId.Empty;
  /// }
  /// </code>
  /// </example>
  public static TenantId Empty => new(Guid.Empty);

  /// <summary>
  /// Gets a predefined fake tenant ID for testing and development scenarios.
  /// </summary>
  /// <value>
  /// A <see cref="TenantId"/> with a predefined GUID value specifically reserved for testing purposes.
  /// </value>
  /// <remarks>
  /// <para>
  /// This property provides a well-known tenant ID that can be used consistently across
  /// unit tests, integration tests, and development environments where a stable tenant context is needed.
  /// </para>
  /// <para>
  /// The fake tenant ID uses a specific GUID value (00000000-0000-0000-0000-000000000001)
  /// that is easily recognizable in logs and debugging scenarios.
  /// </para>
  /// <para>
  /// This is particularly useful with <see cref="FakeTenantService"/> and other testing infrastructure
  /// to provide consistent tenant context across test scenarios.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Using in tests
  /// [Fact]
  /// public async Task CreateUser_ShouldAssignFakeTenantId() {
  ///   var user = new User {
  ///     TenantId = TenantId.FakeTenantId,
  ///     Email = "test@example.com"
  ///   };
  ///   
  ///   await _repository.CreateAsync(user);
  ///   
  ///   var retrieved = await _repository.GetAsync(user.Id);
  ///   Assert.Equal(TenantId.FakeTenantId, retrieved.TenantId);
  /// }
  /// 
  /// // Using with FakeTenantService
  /// services.AddSingleton&lt;ITenantService, FakeTenantService&gt;();
  /// // FakeTenantService.GetCurrentTenantId() returns TenantId.FakeTenantId
  /// 
  /// // Recognizable in logs
  /// _logger.LogInformation("Processing order for tenant: {TenantId}", TenantId.FakeTenantId);
  /// // Outputs: "Processing order for tenant: TenantId-00000000-0000-0000-0000-000000000001"
  /// </code>
  /// </example>
  /// <seealso cref="FakeTenantService"/>
  /// <seealso cref="Empty"/>
  public static TenantId FakeTenantId => new(Guid.Parse("00000000-0000-0000-0000-000000000001", CultureInfo.InvariantCulture));
}
