using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NSI.Domains.StrongIdentifier;

namespace NSI.EntityFramework.Converters;

/// <summary>
/// EF Core value converter enabling persistence of strongly-typed identifiers as their
/// underlying primitive values.
/// </summary>
/// <typeparam name="TId">Concrete strongly-typed identifier type.</typeparam>
/// <typeparam name="TUnderlying">Underlying primitive database type.</typeparam>
/// <remarks>
/// <para>
/// Bridges the domain model (using <see cref="StronglyTypedId{TId, TUnderlying}"/> for type safety)
/// and the relational model (which stores only the primitive value). Applies symmetric conversion
/// delegates for read and write paths.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>To provider: extracts <c>.Value</c> from the strongly-typed id.</description></item>
///   <item><description>From provider: constructs a new strongly-typed id from the primitive.</description></item>
///   <item><description>Null handling is delegated to EF Core (ids should be non-null for keys).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use <see cref="StronglyTypedIdEfCoreExtensions.ApplyStronglyTypedIdConversions"/> to register automatically.</description></item>
///   <item><description>Do not register manually unless you need override precedence.</description></item>
///   <item><description>Ensure identifier type exposes a public constructor (primary or positional) accepting <typeparamref name="TUnderlying"/>.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Constructor invocation compiled once into a cached delegate (no per-row reflection).</description></item>
///   <item><description>Conversion delegates are allocation-free and JIT inlined in most scenarios.</description></item>
///   <item><description>Static initialization cost is O(1) per closed generic pair.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Fully thread-safe; all state is immutable after static initialization.</para>
/// <para>Reflection Justification: A single constructor lookup (expression compiled) during static
/// initialization is required because generic parameter <typeparamref name="TId"/> is unknown at
/// compile time. This complies with the reflection standards (last‑resort dynamic discovery, cached).</para>
/// </remarks>
/// <example>
/// <code>
/// modelBuilder.ApplyStronglyTypedIdConversions(); // Global registration
/// // or manually:
/// modelBuilder.Entity&lt;User&gt;()
///   .Property(u => u.Id)
///   .HasConversion(new StronglyTypedIdValueConverter&lt;UserId, Guid&gt;());
/// </code>
/// </example>
public sealed class StronglyTypedIdValueConverter<TId, TUnderlying>: ValueConverter<TId, TUnderlying>
  where TId : StronglyTypedId<TId, TUnderlying> {
  // Cached factory delegate (compiled once) ---------------------------------
  private static readonly Func<TUnderlying, TId> Factory = CreateFactory();

  private static Func<TUnderlying, TId> CreateFactory() {
    // Attempt to find a single-parameter constructor (primary / positional) accepting TUnderlying
    var ctor = typeof(TId).GetConstructor([typeof(TUnderlying)]);
    if (ctor is not null) {
      var param = Expression.Parameter(typeof(TUnderlying), "value");
      var body = Expression.New(ctor, param);
      var lambda = Expression.Lambda<Func<TUnderlying, TId>>(body, param);
      return lambda.Compile();
    }
    throw new InvalidOperationException(
      $"Type '{typeof(TId).Name}' must expose a public constructor accepting '{typeof(TUnderlying).Name}'.");
  }

  /// <summary>Initializes the converter with compiled delegates.</summary>
  /// <remarks>Delegates are allocation-free and reuse the cached <see cref="Factory"/>.</remarks>
  public StronglyTypedIdValueConverter()
    : base(id => id.Value, value => Factory(value)) { }
}
