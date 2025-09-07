namespace NSI.Domains;

/// <summary>
/// Abstract base class for domain entities with built-in auditing fields.
///
/// Arguments:
/// - CreatedOn (DateTime?): creation timestamp.
/// - CreatedBy (UserId?): creator identifier.
/// - ModifiedOn (DateTime?): last modification timestamp.
/// - ModifiedBy (UserId?): last modifier identifier.
/// </summary>
/// <remarks>
/// Implements <see cref="Audit.IAuditableEntity"/> to enable automatic audit tracking
/// by the persistence layer.
/// Use <see cref="Entity{TId}"/> for strongly-typed identifiers,
/// or inherit directly for custom key strategies.
/// </remarks>
/// <summary>
/// Base entity implementation providing audit fields for creation and modification tracking.
/// </summary>
public abstract class Entity: IEntity {

  /// <summary>UTC date/time when the entity was created.</summary>
  public DateTime? CreatedOn { get; set; }

  /// <summary>Identifier of the user who created the entity.</summary>
  public UserId? CreatedBy { get; set; }

  /// <summary>UTC date/time when the entity was last modified.</summary>
  public DateTime? ModifiedOn { get; set; }

  /// <summary>Identifier of the user who last modified the entity.</summary>
  public UserId? ModifiedBy { get; set; }

}
