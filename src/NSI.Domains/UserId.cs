using NSI.Domains.StrongIdentifier;

namespace NSI.Domains;

/// <summary>
/// Strongly-typed identifier for the User aggregate (wraps a <see cref="Guid"/>).
/// </summary>
/// <remarks>
/// <para>
/// Provides compile-time safety versus raw <c>Guid</c> usage and integrates with the generic
/// <see cref="StronglyTypedId{TId, TUnderlying}"/> infrastructure (parsing, serialization,
/// EF / JSON converters, logging consistency).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Equality / hash code delegate to the underlying <see cref="Guid"/>.</description></item>
///   <item><description>Serialized form: <c>UserId-{Guid}</c> (see <see cref="StronglyTypedId{TId, TUnderlying}.ToString()"/>).</description></item>
///   <item><description><see cref="Empty"/> represents the uninitialized / sentinel value.</description></item>
///   <item><description><see cref="SystemUser"/> reserved for system / background operations.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use <see cref="New()"/> for new identifiers (encapsulates <see cref="Guid.NewGuid()"/>).</description></item>
///   <item><description>Avoid persisting <see cref="Empty"/>; treat as invalid in validation layers.</description></item>
///   <item><description>Prefer passing <c>UserId</c> directly instead of raw <c>Guid</c> parameters.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Immutable (record) and safe to cache / reuse.</para>
/// <para>Performance: Zero allocations beyond the struct-sized <see cref="Guid"/>; static singletons
/// (<see cref="Empty"/>, <see cref="SystemUser"/>) avoid repeated parsing / construction.</para>
/// </remarks>
/// <example>
/// <code>
/// // Create a new user identifier
/// var userId = UserId.New();
///
/// // Serialize / deserialize round-trip
/// var text = UserId.ToSerializedString(userId);
/// if (UserId.TryParse(text, CultureInfo.InvariantCulture, out var parsed)) {
///   // parsed == userId
/// }
///
/// // Use in an entity
/// public sealed class User: Entity&lt;UserId&gt; {
///   public required string Email { get; init; }
/// }
/// </code>
/// </example>
/// <seealso cref="StronglyTypedId{TId, TUnderlying}"/>
/// <seealso cref="Entity{TId}"/>
public sealed record UserId(Guid Value): StronglyTypedId<UserId, Guid>(Value) {
  /// <summary>Singleton empty (all zero) user identifier.</summary>
  public static readonly UserId Empty = new(Guid.Empty);

  /// <summary>Singleton reserved system user identifier (invariant, non-personal).</summary>
  public static readonly UserId SystemUser = new(new Guid("00000000-0000-0000-0000-000000000001"));

  /// <summary>Creates a new random user identifier.</summary>
  /// <returns>A newly generated <see cref="UserId"/>.</returns>
  public static UserId New() => new(Guid.NewGuid());
}
