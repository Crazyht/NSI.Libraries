using System.Diagnostics.CodeAnalysis;

namespace NSI.Domains.StrongIdentifier;

/// <summary>
/// Marker interface enabling the strongly‑typed ID pattern (type-safe identifiers).
/// </summary>
/// <remarks>
/// <para>
/// Provides a common constraint for generic APIs (e.g., base entities, repositories,
/// serializers, EF Core converters) to ensure only approved strongly‑typed identifiers
/// are supplied. Prevents mixing primitive <c>Guid</c>/string values across domains and
/// mitigates accidental parameter swaps (compile-time safety over runtime failures).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>No runtime behavior; purely a compile-time contract.</description></item>
///   <item><description>Implemented by lightweight immutable ID types (usually <c>record struct</c> or <c>sealed record</c>).</description></item>
///   <item><description>Acts as generic constraint in <c>Entity&lt;TId&gt;</c>, converters, serializers.</description></item>
///   <item><description>Does not mandate an underlying primitive accessor (handled by base generic type).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Name IDs with <c>Id</c> suffix (e.g., <c>UserId</c>, <c>OrderId</c>).</description></item>
///   <item><description>Prefer <c>record struct</c> for allocation-free value semantics unless reference identity needed.</description></item>
///   <item><description>Expose a single <c>Value</c> property (primitive) via the shared base <c>StronglyTypedId&lt;TId,TUnderlying&gt;</c>.</description></item>
///   <item><description>Keep factories / parsing logic on the concrete ID type (e.g., <c>UserId.New()</c>).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations are immutable; safe to cache and share.</para>
/// <para>Performance: Eliminates boxing / allocations when implemented as value types;
/// overhead is typically one primitive field.</para>
/// </remarks>
/// <example>
/// <code>
/// // Strongly-typed Guid-based identifier
/// public readonly record struct UserId(Guid Value)
///   : StronglyTypedId&lt;UserId, Guid&gt;(Value);
///
/// // Usage in an aggregate root
/// public sealed class User: Entity&lt;UserId&gt; {
///   public string Email { get; private set; } = string.Empty;
///   public User(UserId id, string email): base(id) => Email = email;
/// }
///
/// // Generic repository constraint
/// public interface IRepository&lt;TId, TAggregate&gt;
///   where TId: struct, IStronglyTypedId
///   where TAggregate: Entity&lt;TId&gt; { }
/// </code>
/// </example>
/// <seealso cref="StronglyTypedId{TId, TUnderlying}"/>
/// <seealso cref="Entity{TId}"/>
[SuppressMessage(
  "Minor Code Smell",
  "S4023:Interfaces should not be empty",
  Justification = "Intentional marker interface providing a generic constraint boundary for the strongly-typed ID pattern; adding members would force all ID types to carry unneeded API surface.")]
public interface IStronglyTypedId { }
