namespace NSI.Domains.Audit;

/// <summary>
/// Read-only audit metadata contract for projection / query models.
/// </summary>
/// <remarks>
/// <para>
/// Represents immutable audit state (creation &amp; modification) exposed to read layers
/// (DTOs / query models). Unlike <see cref="IAuditableEntity"/>, setters are omitted
/// so application code cannot mutate audit data outside persistence boundaries.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="CreatedOn"/> / <see cref="CreatedBy"/> populated once at creation.</description></item>
///   <item><description><see cref="ModifiedOn"/> / <see cref="ModifiedBy"/> reflect last persisted change.</description></item>
///   <item><description>All members may be <c>null</c> if source entity not yet persisted / materialized.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use in read models, API representations, reporting projections.</description></item>
///   <item><description>Do not derive business rules from timestamps (prefer domain events).</description></item>
///   <item><description>Convert to local time zones only at presentation boundary.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Interface is passive; implementing objects typically immutable or
/// snapshot instances per request scope.</para>
/// <para>Performance: Pure data contract; no behavioral overhead.</para>
/// </remarks>
/// <seealso cref="IAuditableEntity"/>
/// <seealso cref="UserId"/>
public interface IAuditableModel {
  /// <summary>UTC timestamp of creation (null if not persisted / unknown).</summary>
  public DateTime? CreatedOn { get; }

  /// <summary>Identifier of creator (system or user) or null if unknown.</summary>
  public UserId? CreatedBy { get; }

  /// <summary>UTC timestamp of last modification (null if never modified).</summary>
  public DateTime? ModifiedOn { get; }

  /// <summary>Identifier of last modifier (system or user) or null if unknown.</summary>
  public UserId? ModifiedBy { get; }
}
