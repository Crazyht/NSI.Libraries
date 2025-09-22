namespace NSI.Domains;

/// <summary>
/// Abstract auditable domain entity base with standard creation / modification metadata.
/// </summary>
/// <remarks>
/// <para>
/// Provides common audit fields required by infrastructure (e.g. EF Core save interceptors)
/// to track lifecycle events. Inherit from <see cref="Entity{TId}"/> for strongly-typed
/// identifier support, or from this base for custom key strategies (composite keys, etc.).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="CreatedOn"/> / <see cref="CreatedBy"/> set once at initial persistence.</description></item>
///   <item><description><see cref="ModifiedOn"/> / <see cref="ModifiedBy"/> updated on each successful update.</description></item>
///   <item><description>Unset (<c>null</c>) values indicate transient (not yet persisted) state.</description></item>
///   <item><description>Audit fields are infrastructure concerns – exclude from domain equality.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Do not mutate audit fields manually in domain logic.</description></item>
///   <item><description>Expose audit data only when required in read models / APIs.</description></item>
///   <item><description>Use UTC for all timestamps (enforced by infrastructure layer).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Not thread-safe; instances are unit-of-work scoped aggregates.</para>
/// <para>Performance: Overhead limited to four nullable property assignments per modification.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Product: Entity {
///   public Guid Id { get; init; }
///   public string Name { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
public abstract class Entity: IEntity {
  /// <summary>UTC timestamp of initial persistence.</summary>
  public DateTime? CreatedOn { get; set; }

  /// <summary>User or system identifier that created the entity.</summary>
  public UserId? CreatedBy { get; set; }

  /// <summary>UTC timestamp of last persisted modification.</summary>
  public DateTime? ModifiedOn { get; set; }

  /// <summary>User or system identifier that performed the last modification.</summary>
  public UserId? ModifiedBy { get; set; }
}
