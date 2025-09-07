namespace NSI.Domains.Audit;

/// <summary>
/// Interface for entities that require audit tracking of creation and modification operations.
/// When implemented, entities will automatically have their audit fields populated during database operations.
/// </summary>
/// <remarks>
/// The DbContext automatically sets these properties during SaveChanges operations:
/// - When an entity is created, <see cref="CreatedOn"/> and <see cref="CreatedBy"/> are set
/// - When an entity is modified, <see cref="ModifiedOn"/> and <see cref="ModifiedBy"/> are set
/// </remarks>
/// <example>
/// <code>
/// public class Product : IAuditableEntity 
/// {
///     public Guid Id { get; set; }
///     public string Name { get; set; }
///     
///     // IAuditableEntity implementation
///     public DateTime? CreatedOn { get; set; }
///     public UserId? CreatedBy { get; set; }
///     public DateTime? ModifiedOn { get; set; }
///     public UserId? ModifiedBy { get; set; }
/// }
/// </code>
/// </example>
public interface IAuditableEntity {
  /// <summary>
  /// Gets or sets the date and time when the entity was created.
  /// This is automatically set by the database context during entity creation.
  /// </summary>
  public DateTime? CreatedOn { get; set; }

  /// <summary>
  /// Gets or sets the ID of the user who created the entity.
  /// This is automatically set by the database context during entity creation.
  /// </summary>
  public UserId? CreatedBy { get; set; }

  /// <summary>
  /// Gets or sets the date and time when the entity was last modified.
  /// This is automatically set by the database context during entity updates.
  /// </summary>
  public DateTime? ModifiedOn { get; set; }

  /// <summary>
  /// Gets or sets the ID of the user who last modified the entity.
  /// This is automatically set by the database context during entity updates.
  /// </summary>
  public UserId? ModifiedBy { get; set; }
}
