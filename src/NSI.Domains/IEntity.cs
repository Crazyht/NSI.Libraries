using NSI.Domains.Audit;

namespace NSI.Domains;

/// <summary>
/// Contract for audited domain entities exposing standard lifecycle metadata.
/// </summary>
/// <remarks>
/// <para>
/// Combines the audit contract (<see cref="IAuditableEntity"/>) with a semantic marker for
/// domain entities processed by persistence / unit-of-work infrastructure.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Audit properties (<see cref="IAuditableEntity.CreatedOn"/>, etc.) are infrastructure-owned.</description></item>
///   <item><description>Implementations may add identifiers (e.g. strongly‑typed IDs) and domain state.</description></item>
///   <item><description>Equality should normally be based on the entity's identifier, not audit fields.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Do not mutate audit fields directly; let interceptors / repositories set them.</description></item>
///   <item><description>Exclude audit metadata from domain events and invariants unless explicitly required.</description></item>
///   <item><description>Expose audit data to API/read models only when there is a business need.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations are not thread-safe; treat as unit-of-work scoped.</para>
/// <para>Performance: Only nullable property access; negligible overhead.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Product: IEntity {
///   public ProductId Id { get; init; }
///   public string Name { get; set; } = string.Empty;
///   // Audit (from IAuditableEntity)
///   public DateTime? CreatedOn { get; set; }
///   public UserId? CreatedBy { get; set; }
///   public DateTime? ModifiedOn { get; set; }
///   public UserId? ModifiedBy { get; set; }
/// }
/// </code>
/// </example>
/// <seealso cref="IAuditableEntity"/>
/// <seealso cref="Entity"/>
/// <seealso cref="Entity{TId}"/>
public interface IEntity: IAuditableEntity { }
