using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NSI.Core.Identity;
using NSI.Core.MultiTenants;
using NSI.Domains.Audit;
using NSI.EntityFramework.Converters;

namespace NSI.EntityFramework;

[SuppressMessage("", "S1066", Justification = "Simplification of conditional expressions is not applicable due to readability concerns.")]
[SuppressMessage("", "S134", Justification = "Excessive nesting is necessary for handling complex tenant and audit logic.")]
[SuppressMessage("", "S1541", Justification = "Method complexity is justified by the need to handle multiple entity types and states.")]
[SuppressMessage("", "S3776", Justification = "Cognitive complexity is acceptable given the multi-tenant and audit requirements.")]
[SuppressMessage("", "S4462", Justification = "Static fields in generic types are used for caching reflection data to improve performance.")]

/// <summary>
/// Application database context that provides access to the database and handles
/// auditing capabilities for tracking entity creation and modification.
/// </summary>
/// <param name="options">The database context options used to configure the context.</param>
/// <param name="userAccessor">Service for accessing the current user information.</param>
/// <param name="timeProvider">Service for providing consistent time values.</param>
/// <param name="tenantService">Service for determining the current tenant context.</param>
public class NsiDbContext<TUser>(
  DbContextOptions options,
  IUserAccessor userAccessor,
  TimeProvider timeProvider,
  ITenantService tenantService): DbContext(options) {

  private readonly TenantId? _TenantId = tenantService.GetCurrentTenantId();
  public TenantId? GetCurrentTenantId() => _TenantId;

  /// <summary>
  /// Configures the model that was discovered by convention from the entity types
  /// exposed in <see cref="DbSet{TEntity}"/> properties on this context.
  /// </summary>
  /// <param name="modelBuilder">The builder being used to construct the model.</param>
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);
    base.OnModelCreating(modelBuilder);
    // Apply converters for strongly-typed IDs
    modelBuilder.ApplyStronglyTypedIdConversions();

    ConfigureAuditableEntities(modelBuilder);
    ConfigureEntitiesWithTenantId(modelBuilder, _TenantId);
  }

  /// <summary>
  /// Configures multi-tenancy query filters and required properties for entities 
  /// that implement <see cref="IHaveTenantId"/>.
  /// </summary>
  /// <param name="modelBuilder">The builder being used to construct the model.</param>
  /// <param name="tenantId">The current tenant ID to use in query filters.</param>
  /// <remarks>
  /// This method sets up automatic query filtering based on tenant ID, ensuring data isolation
  /// between tenants in a multi-tenant environment.
  /// </remarks>
  private static void ConfigureEntitiesWithTenantId(ModelBuilder modelBuilder, TenantId? tenantId) {
    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType).Where(EntityHaveTenant)) {
      modelBuilder.Entity(clrType).Property(nameof(IHaveTenantId.TenantId)).IsRequired();
    }
    if (tenantId == null) {
      // If tenantId is null, it indicates that the connected user is an overtenant account and therefore does not require a query filter.
      return;
    }
    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType).Where(EntityHaveTenant)) {
      var parameter = Expression.Parameter(clrType, "e");
      var property = Expression.Property(parameter, nameof(IHaveTenantId.TenantId));
      var tenantIdConstant = Expression.Constant(tenantId);
      var comparaison = Expression.Equal(property, tenantIdConstant);
      var lambda = Expression.Lambda(comparaison, parameter);
      modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }
    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType).Where(EntityHaveNullableTenant)) {
      var parameter = Expression.Parameter(clrType, "e");
      var property = Expression.Property(parameter, nameof(IHaveNullableTenantId.TenantId));
      var tenantIdConstant = Expression.Constant(tenantId, property.Type);
      var equalExpr = Expression.Equal(property, tenantIdConstant);// e.TenantId == tenantId
      var nullExpr = Expression.Equal(property, Expression.Constant(null, property.Type));// e.TenantId == null
      var orExpr = Expression.OrElse(nullExpr, equalExpr);// e.TenantId == null || e.TenantId == tenantId
      var lambda = Expression.Lambda(orExpr, parameter);
      modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }
  }

  /// <summary>
  /// Configures auditing properties for entities that implement <see cref="IAuditableEntity"/>.
  /// </summary>
  /// <param name="modelBuilder">The builder being used to construct the model.</param>
  /// <remarks>
  /// This method configures required and optional properties related to entity
  /// auditing, such as creation and modification timestamps and user IDs.
  /// </remarks>
  private static void ConfigureAuditableEntities(ModelBuilder modelBuilder) {
    // Configure Auditable Entities
    foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType).Where(EntityHaveAudit)) {
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

  /// <summary>
  /// Determines if the specified CLR type implements the <see cref="IHaveTenantId"/> interface.
  /// </summary>
  /// <param name="clrType">The CLR type to check.</param>
  /// <returns>True if the type implements <see cref="IHaveTenantId"/>; otherwise, false.</returns>
  private static bool EntityHaveTenant(Type clrType) {
    if (clrType == null) {
      return false;
    }

    // Must implement IHaveTenantId (directly or via inheritance)
    var hasTenantId = clrType.IsAssignableTo(typeof(IHaveTenantId));

    // Must NOT implement IHaveInheritedTenantId (directly or via inheritance)
    var hasInheritedTenantId = clrType.IsAssignableTo(typeof(IHaveInheritedTenantId));

    return hasTenantId && !hasInheritedTenantId;
  }

  /// <summary>
  /// Determines if the specified CLR type implements the <see cref="IHaveNullableTenantId"/> interface.
  /// </summary>
  /// <param name="clrType">The CLR type to check.</param>
  /// <returns>True if the type implements <see cref="IHaveNullableTenantId"/>; otherwise, false.</returns>
  private static bool EntityHaveNullableTenant(Type clrType) => clrType?.IsAssignableTo(typeof(IHaveNullableTenantId)) ?? false;

  /// <summary>
  /// Determines if the specified CLR type implements the <see cref="IAuditableEntity"/> interface.
  /// </summary>
  /// <param name="clrType">The CLR type to check.</param>
  /// <returns>True if the type implements <see cref="IAuditableEntity"/>; otherwise, false.</returns>
  private static bool EntityHaveAudit(Type clrType) => clrType?.IsAssignableTo(typeof(IAuditableEntity)) ?? false;

  /// <summary>
  /// Saves all changes made in this context to the database.
  /// </summary>
  /// <param name="acceptAllChangesOnSuccess">Indicates whether <see cref="ChangeTracker.AcceptAllChanges"/> is called after the changes have been sent successfully to the database.</param>
  /// <returns>The number of state entries written to the database.</returns>
  public override int SaveChanges(bool acceptAllChangesOnSuccess) {
    ApplyContextualChangesAsync().GetAwaiter().GetResult();
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  /// <summary>
  /// Saves all changes made in this context to the database asynchronously.
  /// </summary>
  /// <param name="acceptAllChangesOnSuccess">Indicates whether <see cref="ChangeTracker.AcceptAllChanges"/> is called after the changes have been sent successfully to the database.</param>
  /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
  /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
  public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
    await ApplyContextualChangesAsync();
    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  #region Application Contextual Changes

  /// <summary>
  /// Applies contextual changes (tenant isolation and audit information) to entities 
  /// before they are saved to the database.
  /// </summary>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <remarks>
  /// This method orchestrates the application of tenant enforcement and audit information
  /// in the correct order, ensuring data isolation and proper tracking of entity changes.
  /// </remarks>
  private async Task ApplyContextualChangesAsync() {
    await ApplyTenantIdEnforcementAsync();
    await ApplyAuditInformationAsync();
  }

  /// <summary>
  /// Enforces tenant isolation for entities implementing <see cref="IHaveTenantId"/> 
  /// and validates tenant context for <see cref="IHaveNullableTenantId"/> entities.
  /// </summary>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <exception cref="UnauthorizedAccessException">
  /// Thrown when an entity belongs to a different tenant than the current context
  /// or when an <see cref="IHaveTenantId"/> entity is created without a tenant in over-tenant mode.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method enforces multi-tenant data isolation by:
  /// <list type="bullet">
  ///   <item><description>Setting tenant ID for new entities that don't have one specified</description></item>
  ///   <item><description>Validating that all entity operations respect tenant boundaries</description></item>
  ///   <item><description>Preventing tenant switching on existing entities</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// For over-tenant accounts (_TenantId == null), enforcement is skipped except for
  /// validating that IHaveTenantId entities have a tenant specified.
  /// </para>
  /// </remarks>
  private async Task ApplyTenantIdEnforcementAsync() {

    // Single enumeration through all entries, handling different interface types
    foreach (var entry in ChangeTracker.Entries()) {
      var entityTypeName = entry.Entity.GetType().Name;

      // Handle IHaveTenantId entities (including those with IHaveInheritedTenantId)
      if (entry.Entity is IHaveTenantId tenantEntity) {

        if (entry.State == EntityState.Added) {
          await HandleTenantIdForAddedEntityAsync(entry, entityTypeName);

        } else if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted) {
          await HandleTenantIdForModifiedOrDeletedEntityAsync(tenantEntity, entityTypeName, entry.State);
        }
      }
      // Handle IHaveNullableTenantId entities (that are NOT IHaveTenantId)
      else if (entry.Entity is IHaveNullableTenantId nullableTenantEntity) {
        if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted) {
          if (nullableTenantEntity.TenantId == null || nullableTenantEntity.TenantId.Equals(_TenantId)) {
            await HandleNullableTenantIdValidationAsync(nullableTenantEntity, entityTypeName, entry.State);
          } else {
            throw new UnauthorizedAccessException($"Entity {entityTypeName} must belong to the current tenant context.");
          }
        }
      }
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Handles tenant ID enforcement for entities being added to the context.
  /// </summary>
  /// <param name="entry">The entity entry being processed.</param>
  /// <param name="entityTypeName">The name of the entity type for error messages.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  private async Task HandleTenantIdForAddedEntityAsync(EntityEntry entry, string entityTypeName) {
    var tenantIdProperty = entry.Property(nameof(IHaveTenantId.TenantId));
    var currentTenantId = (TenantId?)tenantIdProperty.CurrentValue;

    if (currentTenantId == null || currentTenantId.Equals(TenantId.Empty)) {
      // TenantId not specified
      if (_TenantId == null) {
        // Over-tenant account - IHaveTenantId entities must have a tenant
        throw new UnauthorizedAccessException(
          $"Entity {entityTypeName} must have a tenant ID specified when created in over-tenant context.");
      }

      // Set the current tenant ID
      tenantIdProperty.CurrentValue = _TenantId;
    } else {
      // TenantId specified - validate it matches current context
      if (_TenantId != null && !currentTenantId.Equals(_TenantId)) {
        throw new UnauthorizedAccessException(
          $"Entity {entityTypeName} must belong to the current tenant context.");
      }
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Handles tenant ID validation for entities being modified or deleted.
  /// </summary>
  /// <param name="tenantEntity">The entity implementing IHaveTenantId.</param>
  /// <param name="entityTypeName">The name of the entity type for error messages.</param>
  /// <param name="entityState">The current state of the entity.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  private async Task HandleTenantIdForModifiedOrDeletedEntityAsync(IHaveTenantId tenantEntity, string entityTypeName, EntityState entityState) {
    // Skip validation for over-tenant accounts
    if (_TenantId == null) {
      await Task.CompletedTask;
      return;
    }

    var currentTenantId = tenantEntity.TenantId;

    if (!currentTenantId.Equals(_TenantId)) {
      throw new UnauthorizedAccessException(
        $"Entity {entityTypeName} belongs to a different tenant and cannot be {entityState.ToString()} in current context.");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Handles tenant ID validation for nullable tenant entities.
  /// </summary>
  /// <param name="nullableTenantEntity">The entity implementing IHaveNullableTenantId.</param>
  /// <param name="entityTypeName">The name of the entity type for error messages.</param>
  /// <param name="entityState">The current state of the entity.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  private async Task HandleNullableTenantIdValidationAsync(IHaveNullableTenantId nullableTenantEntity, string entityTypeName, EntityState entityState) {

    // Skip validation for over-tenant accounts
    if (_TenantId == null) {
      await Task.CompletedTask;
      return;
    }

    var currentTenantId = nullableTenantEntity.TenantId;

    // If TenantId is specified, validate it matches current context
    if (currentTenantId != null && !currentTenantId.Equals(_TenantId)) {
      throw new UnauthorizedAccessException(
        $"Entity {entityTypeName} belongs to a different tenant and cannot be {entityState.ToString()} in current context.");
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Applies audit information to entities implementing <see cref="IAuditableEntity"/> 
  /// before they are saved to the database.
  /// </summary>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <remarks>
  /// This method sets creation and modification timestamps and user information
  /// for entities that support auditing capabilities.
  /// </remarks>
  private async Task ApplyAuditInformationAsync() {
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var userInfo = await userAccessor.GetCurrentUserInfoAsync();

    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>()) {
      if (entry.State == EntityState.Added) {
        entry.Entity.CreatedOn = now;
        entry.Entity.CreatedBy = userInfo.Id;
        entry.Entity.ModifiedOn = null;
        entry.Entity.ModifiedBy = null;
      } else if (entry.State == EntityState.Modified) {
        entry.Entity.ModifiedOn = now;
        entry.Entity.ModifiedBy = userInfo.Id;
      } else {
        // Nothing to do for Deleted or Unchanged states
      }
    }
  }
}

#endregion
