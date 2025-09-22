namespace NSI.Domains.Audit;

/// <summary>
/// Contract for entities whose lifecycle (create/modify) must be audited.
/// </summary>
/// <remarks>
/// <para>
/// Infrastructure (e.g., EF Core interceptors) populates audit fields automatically
/// during persistence. Domain code should not mutate these outside of seeding/import
/// routines to preserve integrity of audit history.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="CreatedOn"/> / <see cref="CreatedBy"/> set once on initial insert.</description></item>
///   <item><description><see cref="ModifiedOn"/> / <see cref="ModifiedBy"/> updated on each successful update.</description></item>
///   <item><description>Values remain <c>null</c> for transient (not yet persisted) entities.</description></item>
///   <item><description>Implementations must expose writable setters for infrastructure assignment.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Exclude audit properties from equality or domain invariants.</description></item>
///   <item><description>Expose audit data in read models / DTOs only when required by UI.</description></item>
///   <item><description>Prefer UTC timestamps (enforced by infrastructure layer).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Entity instances are not thread-safe; treat as unit-of-work scoped state.</para>
/// <para>Performance: Overhead limited to simple property assignments per tracked modification.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Product: IAuditableEntity {
///   public Guid Id { get; init; }
///   public string Name { get; set; } = string.Empty;
///   public DateTime? CreatedOn { get; set; }
///   public UserId? CreatedBy { get; set; }
///   public DateTime? ModifiedOn { get; set; }
///   public UserId? ModifiedBy { get; set; }
/// }
/// </code>
/// </example>
/// <seealso cref="UserId"/>
public interface IAuditableEntity {
  /// <summary>UTC timestamp of initial persistence.</summary>
  public DateTime? CreatedOn { get; set; }

  /// <summary>User/system identifier that created the entity.</summary>
  public UserId? CreatedBy { get; set; }

  /// <summary>UTC timestamp of last persisted modification.</summary>
  public DateTime? ModifiedOn { get; set; }

  /// <summary>User/system identifier that performed last modification.</summary>
  public UserId? ModifiedBy { get; set; }
}
