using NSI.Domains.StrongIdentifier;

namespace NSI.Domains;

/// <summary>
/// Abstract base for domain entities with a strongly-typed identifier and audit metadata.
/// </summary>
/// <typeparam name="TId">Strongly-typed identifier implementing <see cref="IStronglyTypedId"/>.</typeparam>
/// <remarks>
/// <para>
/// Extends <see cref="Entity"/> by adding a required strongly-typed primary key. Supports value based
/// identity semantics (two entity instances are equal if their identifiers match). Intended for
/// aggregate roots and entities using single-column surrogate keys.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="Id"/> is required and immutable after construction (init-only).</description></item>
///   <item><description>Equality / hash code rely solely on <see cref="Id"/> (audit fields excluded).</description></item>
///   <item><description>Audit fields inherited from <see cref="Entity"/> are infrastructure concerns.</description></item>
/// </list>
/// </para>
/// <para>Equality:
/// <list type="bullet">
///   <item><description><see cref="Equals(Entity{TId})"/> returns true when identifiers match.</description></item>
///   <item><description><see cref="object.Equals(object?)"/> delegates to the typed overload.</description></item>
///   <item><description><see cref="GetHashCode"/> uses the identifier's hash code.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Derive from <see cref="Entity"/> instead when using composite / natural keys.</description></item>
///   <item><description>Keep domain logic out of equality overrides (identifier only).</description></item>
///   <item><description>Return simple concise strings from <see cref="ToString"/> for logging / diagnostics.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Not thread-safe; instances are scoped to a unit-of-work.</para>
/// <para>Performance: Equality checks are O(1) identifier comparisons; hash generation delegates to
/// the underlying identifier type.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Product: Entity&lt;ProductId&gt; {
///   public required string Name { get; init; }
/// }
///
/// public readonly record struct ProductId(Guid Value)
///   : StronglyTypedId&lt;ProductId, Guid&gt;(Value);
/// </code>
/// </example>
public abstract class Entity<TId>: Entity, IEquatable<Entity<TId>>
  where TId : notnull, IStronglyTypedId, IEquatable<TId> {
  /// <summary>
  /// Gets the immutable strongly-typed identifier for this entity instance.
  /// </summary>
  /// <remarks>Assigned during construction / materialization and never changed thereafter.</remarks>
  public required TId Id { get; init; }

  /// <summary>
  /// Determines whether this instance and another entity have the same identifier.
  /// </summary>
  /// <param name="other">Other entity instance to compare.</param>
  /// <returns><c>true</c> if identifiers are equal; otherwise <c>false</c>.</returns>
  public bool Equals(Entity<TId>? other) {
    if (other is null) {
      return false;
    }
    if (ReferenceEquals(this, other)) {
      return true;
    }
    return EqualityComparer<TId>.Default.Equals(Id, other.Id);
  }

  /// <summary>
  /// Determines whether this instance and a specified object are equal by identifier.
  /// </summary>
  /// <param name="obj">Object to compare.</param>
  /// <returns><c>true</c> if <paramref name="obj"/> is an <see cref="Entity&lt;TId&gt;"/> with the same identifier.</returns>
  public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

  /// <summary>
  /// Returns a hash code based on the identifier.
  /// </summary>
  /// <returns>Hash code of <see cref="Id"/>.</returns>
  public override int GetHashCode() => Id.GetHashCode();

  /// <summary>
  /// Returns a concise string representation containing the identifier.
  /// </summary>
  /// <returns>Bracketed identifier string.</returns>
  public override string ToString() => $"[{Id}]";
}
