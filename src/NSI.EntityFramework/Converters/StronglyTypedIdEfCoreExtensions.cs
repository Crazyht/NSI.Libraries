using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NSI.Domains.StrongIdentifier;

namespace NSI.EntityFramework.Converters;

/// <summary>
/// EF Core extensions for auto-configuring value converters for strongly-typed identifiers.
/// </summary>
/// <remarks>
/// <para>
/// Scans the EF Core model and applies <see cref="ValueConverter"/> instances for all properties
/// whose CLR type derives from <see cref="StronglyTypedId{TId, TUnderlying}"/> ensuring they are
/// persisted as their underlying primitive type (e.g. <c>Guid</c>, <c>int</c>, <c>long</c>, <c>string</c>).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Executed during <see cref="DbContext.OnModelCreating(ModelBuilder)"/>.</description></item>
///   <item><description>Id properties remain strongly-typed in domain model; storage stays primitive.</description></item>
///   <item><description>Id conversion is transparent to queries and change tracking.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Invoke once per context (idempotent and safe to repeat).</description></item>
///   <item><description>Place after custom entity configuration so overrides remain effective.</description></item>
///   <item><description>Keep strongly-typed Id types minimal (single value) for optimal mapping.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Reflection occurs only during model building (not per query).</description></item>
///   <item><description>Converter instances cached by EF Core after assignment.</description></item>
///   <item><description>Traversal complexity: O(entityTypes * properties).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Model building is single-threaded by EF Core; no additional safeguards needed.</para>
/// <para>Reflection Justification: Runtime discovery of generic arguments is required; expression-based
/// compile-time enumeration (preferred in guidelines) is not possible for arbitrary model graphs.
/// This usage aligns with the "last resort dynamic" clause in reflection standards.</para>
/// </remarks>
/// <example>
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder) {
///   base.OnModelCreating(modelBuilder);
///   modelBuilder.ApplyStronglyTypedIdConversions();
/// }
/// </code>
/// </example>
public static class StronglyTypedIdEfCoreExtensions {
  /// <summary>
  /// Applies value converters to all model properties typed as <see cref="StronglyTypedId{TId, TUnderlying}"/>.
  /// </summary>
  /// <param name="modelBuilder">Model builder instance (non-null).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelBuilder"/> is null.</exception>
  /// <remarks>
  /// Safe to call multiple times; previously configured properties are simply reassigned same converter.
  /// </remarks>
  public static void ApplyStronglyTypedIdConversions(this ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);
    var baseGeneric = typeof(StronglyTypedId<,>);
    foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
      foreach (var property in entityType.GetProperties()) {
        var clrType = property.ClrType;
        if (TryGetStronglyTypedIdConverter(clrType, baseGeneric, out var converter)) {
          property.SetValueConverter(converter);
        }
      }
    }
  }

  /// <summary>
  /// Attempts to create a value converter for the specified CLR type when it derives from
  /// <see cref="StronglyTypedId{TId, TUnderlying}"/>.
  /// </summary>
  /// <param name="clr">CLR type to evaluate.</param>
  /// <param name="baseGeneric">Closed generic definition (<see cref="StronglyTypedId{TId, TUnderlying}"/>).</param>
  /// <param name="converter">Resulting converter if successful; otherwise null.</param>
  /// <returns><c>true</c> if a converter was created; otherwise <c>false</c>.</returns>
  /// <remarks>
  /// Traverses the inheritance chain to locate the generic base type, extracts the concrete id and
  /// underlying primitive types, then constructs <see cref="StronglyTypedIdValueConverter{TId, TUnderlying}"/>.
  /// </remarks>
  private static bool TryGetStronglyTypedIdConverter(
    Type clr,
    Type baseGeneric,
    out ValueConverter? converter) {
    converter = null;
    var current = clr;
    while (current != null) {
      if (current.IsGenericType && current.GetGenericTypeDefinition() == baseGeneric) {
        var args = current.GetGenericArguments();
        var idType = clr;            // the derived strongly-typed id
        var underlyingType = args[1];
        var converterType = typeof(StronglyTypedIdValueConverter<,>).MakeGenericType(idType, underlyingType);
        converter = (ValueConverter)Activator.CreateInstance(converterType)!;
        return true;
      }
      current = current.BaseType;
    }
    return false;
  }
}
