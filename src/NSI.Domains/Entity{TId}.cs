using NSI.Domains.StrongIdentifier;

namespace NSI.Domains;

/// <summary>
/// Abstract base class for domain entities with a strongly-typed identifier
/// and built-in auditing.
///
/// Arguments:
/// - Id (TId): unique strongly-typed identifier, init-only.
/// - Equals(Entity&lt;TId&gt;): value equality by Id.
/// - Equals(object?): value equality by Id.
/// - GetHashCode(): hash based on Id.
/// - ToString(): string representation using Id.
/// </summary>
/// <typeparam name="TId">
/// Strongly-typed identifier type implementing <see cref="IStronglyTypedId"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
/// <remarks>
/// Inherits audit fields from <see cref="Entity"/>.  
/// For composite keys, inherit directly from <see cref="Entity"/>.
/// </remarks>
public abstract class Entity<TId>: Entity, IEquatable<Entity<TId>>
  where TId : notnull, IStronglyTypedId, IEquatable<TId> {

  /// <summary>Unique strongly-typed identifier for this entity.</summary>
  public required TId Id { get; init; }

  /// <summary>Checks value equality against another <see cref="Entity{TId}"/>.</summary>
  public bool Equals(Entity<TId>? other) {
    if (other is null) {
      return false;
    }
    if (ReferenceEquals(this, other)) {
      return true;
    }
    return EqualityComparer<TId>.Default.Equals(Id, other.Id);
  }

  /// <summary>Checks value equality against another object.</summary>
  public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

  /// <summary>Gets hash code based on the Id.</summary>
  public override int GetHashCode() => Id.GetHashCode();

  /// <summary>Returns a string representation using the Id.</summary>
  public override string ToString() => $"[{Id}]";
}
