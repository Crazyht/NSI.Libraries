namespace NSI.Core.MultiTenants;

/// <summary>
/// Defines an entity that belongs to a specific tenant in a multi-tenant system with non-nullable tenant identification.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that must always be scoped to a specific tenant.
/// This enables automatic tenant-based filtering and data isolation in multi-tenant applications,
/// ensuring that entities cannot exist without a tenant context.
/// </para>
/// <para>
/// Key characteristics and use cases:
/// <list type="bullet">
///   <item><description>Mandatory tenant scope: Entities must always belong to a specific tenant</description></item>
///   <item><description>Automatic filtering: ORM configurations can automatically filter queries by tenant</description></item>
///   <item><description>Data isolation: Prevents accidental cross-tenant data access</description></item>
///   <item><description>Security enforcement: Ensures tenant boundaries are maintained at the domain level</description></item>
/// </list>
/// </para>
/// <para>
/// This interface is typically used for core business entities like users, orders, products, etc.,
/// where tenant isolation is critical for security and data integrity.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Domain entity implementation
/// public class User: IHaveTenantId {
///   public UserId Id { get; init; } = UserId.New();
///   public TenantId TenantId { get; init; }
///   public string Email { get; init; } = string.Empty;
///   public string Name { get; init; } = string.Empty;
/// }
/// 
/// // Repository with tenant filtering
/// public class UserRepository {
///   private readonly ITenantService _tenantService;
///   private readonly DbContext _context;
///   
///   public async Task&lt;User?&gt; GetUserAsync(UserId userId) {
///     var currentTenant = _tenantService.GetCurrentTenantId();
///     
///     return await _context.Users
///       .Where(u => u.TenantId == currentTenant &amp;&amp; u.Id == userId)
///       .FirstOrDefaultAsync();
///   }
/// }
/// 
/// // Entity Framework configuration
/// protected override void OnModelCreating(ModelBuilder modelBuilder) {
///   modelBuilder.Entity&lt;User&gt;()
///     .HasQueryFilter(u => u.TenantId == _tenantService.GetCurrentTenantId());
/// }
/// </code>
/// </example>
/// <seealso cref="IHaveNullableTenantId"/>
/// <seealso cref="IHaveInheritedTenantId"/>
/// <seealso cref="TenantId"/>
/// <seealso cref="ITenantService"/>
public interface IHaveTenantId {

  /// <summary>
  /// Gets the tenant identifier this entity belongs to.
  /// </summary>
  /// <value>
  /// A strongly-typed tenant identifier that cannot be null.
  /// This property ensures the entity always has a valid tenant context.
  /// </value>
  /// <remarks>
  /// <para>
  /// This property uses the init-only setter pattern to ensure immutability after construction.
  /// The tenant ID should be set during entity creation and never changed afterward to maintain
  /// data consistency and security boundaries.
  /// </para>
  /// </remarks>
  public TenantId TenantId { get; init; }
}

/// <summary>
/// Marker interface used to disable automatic query filtering on child entities of filtered parent entities.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that do not have a direct <see cref="TenantId"/> property
/// but whose tenant context is inherited from a parent entity. By marking an entity with this interface,
/// automatic multi-tenant query filters will not be applied to it, even if its parent entity is filtered.
/// </para>
/// <para>
/// Key use cases and scenarios:
/// <list type="bullet">
///   <item><description>Aggregate children: Child entities within an aggregate root where tenant filtering occurs at the root level</description></item>
///   <item><description>Value objects: Domain objects that are owned by a tenant-scoped entity but don't need direct tenant filtering</description></item>
///   <item><description>Related entities: Entities that are implicitly tenant-scoped through relationships but don't store tenant ID directly</description></item>
///   <item><description>Performance optimization: Avoiding redundant tenant filters on entities already filtered through joins</description></item>
/// </list>
/// </para>
/// <para>
/// This interface works with Entity Framework query filters and other ORM configurations
/// to prevent automatic application of tenant-scoping rules while still maintaining
/// proper tenant isolation through parent entity relationships.
/// </para>
/// <para>
/// Important: Entities implementing this interface must ensure tenant isolation is maintained
/// through proper relationships with tenant-scoped parent entities. Failing to do so could
/// create security vulnerabilities where cross-tenant data access becomes possible.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Parent entity with tenant filtering
/// public class Order: IHaveTenantId {
///   public OrderId Id { get; init; }
///   public TenantId TenantId { get; init; }
///   public List&lt;OrderItem&gt; Items { get; init; } = new();
/// }
/// 
/// // Child entity that inherits tenant context from parent
/// public class OrderItem: IHaveInheritedTenantId {
///   public OrderItemId Id { get; init; }
///   public OrderId OrderId { get; init; } // Foreign key to tenant-filtered parent
///   public string ProductName { get; init; } = string.Empty;
///   public decimal Price { get; init; }
///   
///   // Navigation property - tenant context inherited through this relationship
///   public Order Order { get; init; } = null!;
/// }
/// 
/// // Entity Framework configuration
/// protected override void OnModelCreating(ModelBuilder modelBuilder) {
///   // Tenant filter applied to parent entity
///   modelBuilder.Entity&lt;Order&gt;()
///     .HasQueryFilter(o => o.TenantId == _tenantService.GetCurrentTenantId());
///   
///   // No tenant filter needed on OrderItem - inherits through relationship
///   // The IHaveInheritedTenantId marker tells the system this is intentional
///   
///   // Relationship configuration ensures tenant isolation
///   modelBuilder.Entity&lt;OrderItem&gt;()
///     .HasOne(oi => oi.Order)
///     .WithMany(o => o.Items)
///     .HasForeignKey(oi => oi.OrderId);
/// }
/// 
/// // Repository query - tenant isolation maintained through join
/// public async Task&lt;List&lt;OrderItem&gt;&gt; GetOrderItemsAsync(OrderId orderId) {
///   return await _context.OrderItems
///     .Include(oi => oi.Order) // Order will be tenant-filtered automatically
///     .Where(oi => oi.OrderId == orderId)
///     .ToListAsync();
/// }
/// 
/// // Alternative: Value object example
/// public class Address: IHaveInheritedTenantId {
///   public string Street { get; init; } = string.Empty;
///   public string City { get; init; } = string.Empty;
///   public string Country { get; init; } = string.Empty;
/// }
/// 
/// public class Customer: IHaveTenantId {
///   public CustomerId Id { get; init; }
///   public TenantId TenantId { get; init; }
///   public Address BillingAddress { get; init; } = null!;
///   public Address ShippingAddress { get; init; } = null!;
/// }
/// </code>
/// </example>
/// <seealso cref="IHaveTenantId"/>
/// <seealso cref="IHaveNullableTenantId"/>
/// <seealso cref="TenantId"/>
public interface IHaveInheritedTenantId { }
