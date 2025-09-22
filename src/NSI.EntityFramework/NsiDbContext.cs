using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NSI.Core.Identity;
using NSI.Core.MultiTenants;
using NSI.Domains.Audit;
using NSI.EntityFramework.Converters;

namespace NSI.EntityFramework;

/// <summary>
/// Generic application <see cref="DbContext"/> adding multi-tenant query isolation and audit field
/// population for entities implementing <see cref="IHaveTenantId"/>, <see cref="IHaveNullableTenantId"/>
/// and <see cref="IAuditableEntity"/> using strongly‑typed identifiers.
/// </summary>
/// <typeparam name="TUser">User entity type referenced by audit foreign keys.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Automatic strongly‑typed id converters (<see cref="StronglyTypedIdEfCoreExtensions"/>).</description></item>
///   <item><description>Global query filters for tenant-scoped entities based on resolved <see cref="TenantId"/>.</description></item>
///   <item><description>Audit creation / modification timestamps &amp; user attribution on SaveChanges.</description></item>
///   <item><description>Over‑tenant (platform) accounts signaled by null tenant id (no query filter applied).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Derive concrete application contexts from this generic base.</description></item>
///   <item><description>Register one scoped instance per unit-of-work / request.</description></item>
///   <item><description>Avoid placing domain logic inside the context; keep orchestration only.</description></item>
///   <item><description>Override <see cref="OnModelCreating(ModelBuilder)"/> AFTER calling base to extend mappings.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Tenant filters compiled once per model cache; per-query reuse by EF.</description></item>
///   <item><description>Audit writes use current UTC timestamp from injected <see cref="TimeProvider"/>.</description></item>
///   <item><description>SaveChanges sync path bridges to async once (accepted pattern per repository rules).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: DbContext instances are not thread-safe. Scope per request or operation.</para>
/// <para>Exceptions: Tenant enforcement raises <see cref="UnauthorizedAccessException"/> for cross-tenant
/// access or missing tenant context where required.</para>
/// </remarks>
public class NsiDbContext<TUser>: DbContext {
  /// <summary>Initializes a new context instance.</summary>
  /// <param name="options">EF Core options (non-null).</param>
  /// <param name="userAccessor">Accessor for current user (non-null).</param>
  /// <param name="timeProvider">Time source abstraction (non-null).</param>
  /// <param name="tenantService">Tenant resolution service (non-null).</param>
  public NsiDbContext(
    DbContextOptions options,
    IUserAccessor userAccessor,
    TimeProvider timeProvider,
    ITenantService tenantService) : base(options) {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(userAccessor);
    ArgumentNullException.ThrowIfNull(timeProvider);
    ArgumentNullException.ThrowIfNull(tenantService);
    _UserAccessor = userAccessor;
    _TimeProvider = timeProvider;
    _TenantId = tenantService.GetCurrentTenantId();
  }

  private readonly IUserAccessor _UserAccessor;
  private readonly TimeProvider _TimeProvider;
  private readonly TenantId? _TenantId;

  /// <summary>Gets the tenant identifier for this context instance (null = over‑tenant scope).</summary>
  public TenantId? GetCurrentTenantId() => _TenantId;

  /// <inheritdoc />
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);
    base.OnModelCreating(modelBuilder);

    // Strongly‑typed id conversions
    modelBuilder.ApplyStronglyTypedIdConversions();

    // Naming conventions may be applied via extension prior to configuration
    ConfigureAuditableEntities(modelBuilder);
    ConfigureEntitiesWithTenantId(modelBuilder, _TenantId);
  }

  /// <summary>
  /// Adds required property constraints and query filters for tenant-scoped entities.
  /// </summary>
  private static void ConfigureEntitiesWithTenantId(ModelBuilder modelBuilder, TenantId? tenantId) {
    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(t => t.ClrType).Where(EntityHaveTenant)) {
      modelBuilder.Entity(clrType).Property(nameof(IHaveTenantId.TenantId)).IsRequired();
    }

    // Over‑tenant (platform) context: no global filters applied.
    if (tenantId == null) {
      return;
    }

    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(t => t.ClrType).Where(EntityHaveTenant)) {
      var p = Expression.Parameter(clrType, "e");
      var body = Expression.Equal(
        Expression.Property(p, nameof(IHaveTenantId.TenantId)),
        Expression.Constant(tenantId));
      modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(body, p));
    }

    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(t => t.ClrType).Where(EntityHaveNullableTenant)) {
      var p = Expression.Parameter(clrType, "e");
      var property = Expression.Property(p, nameof(IHaveNullableTenantId.TenantId));
      var tenantConst = Expression.Constant(tenantId, property.Type);
      var equalExpr = Expression.Equal(property, tenantConst);
      var nullExpr = Expression.Equal(property, Expression.Constant(null, property.Type));
      var orExpr = Expression.OrElse(nullExpr, equalExpr);
      modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(orExpr, p));
    }
  }

  /// <summary>Configures auditing properties for entities implementing <see cref="IAuditableEntity"/>.</summary>
  private static void ConfigureAuditableEntities(ModelBuilder modelBuilder) {
    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(t => t.ClrType).Where(EntityHaveAudit)) {
      modelBuilder.Entity(clrType).Property(nameof(IAuditableEntity.CreatedOn)).IsRequired();
      modelBuilder.Entity(clrType).Property(nameof(IAuditableEntity.CreatedBy)).IsRequired();
      modelBuilder.Entity(clrType).Property(nameof(IAuditableEntity.ModifiedOn));
      modelBuilder.Entity(clrType).Property(nameof(IAuditableEntity.ModifiedBy));
      modelBuilder.Entity(clrType).HasOne(typeof(TUser))
        .WithMany()
        .HasForeignKey(nameof(IAuditableEntity.CreatedBy))
        .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity(clrType).HasOne(typeof(TUser))
        .WithMany()
        .HasForeignKey(nameof(IAuditableEntity.ModifiedBy))
        .OnDelete(DeleteBehavior.Restrict);
    }
  }

  private static bool EntityHaveTenant(Type clrType) =>
    clrType != null && clrType.IsAssignableTo(typeof(IHaveTenantId)) &&
    !clrType.IsAssignableTo(typeof(IHaveInheritedTenantId));

  private static bool EntityHaveNullableTenant(Type clrType) =>
    clrType?.IsAssignableTo(typeof(IHaveNullableTenantId)) ?? false;

  private static bool EntityHaveAudit(Type clrType) =>
    clrType?.IsAssignableTo(typeof(IAuditableEntity)) ?? false;

  /// <inheritdoc />
  public override int SaveChanges(bool acceptAllChangesOnSuccess) {
#pragma warning disable S4462 // Allowed sync bridge pattern
    ApplyContextualChangesAsync().GetAwaiter().GetResult();
#pragma warning restore S4462
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  /// <inheritdoc />
  public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
    await ApplyContextualChangesAsync();
    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  #region Application Contextual Changes
  /// <summary>Applies tenant enforcement and audit metadata prior to persistence.</summary>
  private async Task ApplyContextualChangesAsync() {
    await ApplyTenantIdEnforcementAsync();
    await ApplyAuditInformationAsync();
  }

  /// <summary>Applies multi-tenant invariants to tracked entities.</summary>
  private async Task ApplyTenantIdEnforcementAsync() {
    foreach (var entry in ChangeTracker.Entries()) {
      await ProcessTenantEnforcementEntryAsync(entry);
    }
  }

  private async Task ProcessTenantEnforcementEntryAsync(EntityEntry entry) {
    var entityTypeName = entry.Entity.GetType().Name;

    if (entry.Entity is IHaveTenantId tenantEntity) {
      await HandleTenantIdForAddedOrChangedEntityAsync(entry, tenantEntity, entityTypeName);
      return;
    }

    if (entry.Entity is IHaveNullableTenantId nullableTenantEntity) {
      await ProcessNullableTenantIdEntryAsync(entry, nullableTenantEntity, entityTypeName);
    }
  }

  private Task HandleTenantIdForAddedOrChangedEntityAsync(EntityEntry entry, IHaveTenantId tenantEntity, string entityTypeName) {
    if (entry.State == EntityState.Added) {
      return HandleTenantIdForAddedEntityAsync(entry, entityTypeName);
    }
    if (entry.State is EntityState.Modified or EntityState.Deleted) {
      return HandleTenantIdForModifiedOrDeletedEntityAsync(tenantEntity, entityTypeName, entry.State);
    }
    return Task.CompletedTask;
  }

  private Task ProcessNullableTenantIdEntryAsync(EntityEntry entry, IHaveNullableTenantId nullableTenantEntity, string entityTypeName) {
    if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) {
      return Task.CompletedTask;
    }
    if (_TenantId == null) {
      return Task.CompletedTask; // over‑tenant skip
    }
    if (nullableTenantEntity.TenantId == null || nullableTenantEntity.TenantId.Equals(_TenantId)) {
      return HandleNullableTenantIdValidationAsync(nullableTenantEntity, entityTypeName, entry.State);
    }
    throw new UnauthorizedAccessException($"Entity {entityTypeName} must belong to the current tenant context.");
  }

  private Task HandleTenantIdForAddedEntityAsync(EntityEntry entry, string entityTypeName) {
    var tenantIdProperty = entry.Property(nameof(IHaveTenantId.TenantId));
    var currentTenantId = (TenantId?)tenantIdProperty.CurrentValue;

    if (currentTenantId == null || currentTenantId.Equals(TenantId.Empty)) {
      if (_TenantId == null) {
        throw new UnauthorizedAccessException(
          $"Entity {entityTypeName} must have a tenant ID specified when created in over-tenant context.");
      }
      tenantIdProperty.CurrentValue = _TenantId;
    } else if (_TenantId != null && !currentTenantId.Equals(_TenantId)) {
      throw new UnauthorizedAccessException(
        $"Entity {entityTypeName} must belong to the current tenant context.");
    }

    return Task.CompletedTask;
  }

  private Task HandleTenantIdForModifiedOrDeletedEntityAsync(IHaveTenantId tenantEntity, string entityTypeName, EntityState entityState) {
    if (_TenantId == null) {
      return Task.CompletedTask; // over‑tenant can bypass
    }
    if (!tenantEntity.TenantId.Equals(_TenantId)) {
      throw new UnauthorizedAccessException(
        $"Entity {entityTypeName} belongs to a different tenant and cannot be {entityState} in current context.");
    }
    return Task.CompletedTask;
  }

  private Task HandleNullableTenantIdValidationAsync(IHaveNullableTenantId nullableTenantEntity, string entityTypeName, EntityState entityState) {
    if (_TenantId == null) {
      return Task.CompletedTask;
    }
    var currentTenantId = nullableTenantEntity.TenantId;
    if (currentTenantId != null && !currentTenantId.Equals(_TenantId)) {
      throw new UnauthorizedAccessException(
        $"Entity {entityTypeName} belongs to a different tenant and cannot be {entityState} in current context.");
    }
    return Task.CompletedTask;
  }

  /// <summary>Populates audit fields (Created*/Modified*) for <see cref="IAuditableEntity"/> entries.</summary>
  private async Task ApplyAuditInformationAsync() {
    var now = _TimeProvider.GetUtcNow().UtcDateTime;
    var userInfo = await _UserAccessor.GetCurrentUserInfoAsync();

    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>()) {
      if (entry.State == EntityState.Added) {
        entry.Entity.CreatedOn = now;
        entry.Entity.CreatedBy = userInfo.Id;
        entry.Entity.ModifiedOn = null;
        entry.Entity.ModifiedBy = null;
        continue;
      }
      if (entry.State == EntityState.Modified) {
        entry.Entity.ModifiedOn = now;
        entry.Entity.ModifiedBy = userInfo.Id;
      }
    }
  }
  #endregion
}
