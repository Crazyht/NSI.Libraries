using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NSI.Domains.StrongIdentifier;

namespace NSI.EntityFramework.Converters;
/// <summary>
/// Extension methods for automatically applying strongly-typed ID conversions in Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>
/// This class provides functionality to automatically configure Entity Framework Core to work with
/// strongly-typed IDs derived from <see cref="StronglyTypedId{TId, TUnderlying}"/>. When applied,
/// it ensures that all strongly-typed ID properties throughout your entity model will be properly
/// mapped to their underlying primitive values in the database.
/// </para>
/// <para>
/// Without these converters, Entity Framework Core would attempt to map strongly-typed IDs as complex
/// types rather than as the primitive values they represent, leading to mapping errors or inefficient
/// storage.
/// </para>
/// </remarks>
public static class StronglyTypedIdEfCoreExtensions {
  /// <summary>
  /// Applies value converters to all properties in the model that use strongly-typed IDs.
  /// </summary>
  /// <param name="modelBuilder">The model builder being used to construct the Entity Framework model.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="modelBuilder"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This extension method scans all entity types in the model and automatically applies appropriate
  /// value converters to any properties that use strongly-typed IDs. This enables transparent
  /// persistence of strongly-typed IDs as their underlying primitive values.
  /// </para>
  /// <para>
  /// Call this method in your <see cref="DbContext.OnModelCreating(ModelBuilder)"/> method to
  /// ensure all strongly-typed IDs in your model are properly configured for database persistence.
  /// </para>
  /// </remarks>
  /// <example>
  /// Usage in <c>DbContext.OnModelCreating</c>:
  /// <code>
  /// protected override void OnModelCreating(ModelBuilder modelBuilder)
  /// {
  ///     base.OnModelCreating(modelBuilder);
  ///     
  ///     // Apply converters for all strongly-typed IDs in the model
  ///     modelBuilder.ApplyStronglyTypedIdConversions();
  ///     
  ///     // Continue with other model configurations...
  /// }
  /// </code>
  /// </example>
  public static void ApplyStronglyTypedIdConversions(this ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);
    var baseGeneric = typeof(StronglyTypedId<,>);
    foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
      foreach (var prop in entityType.GetProperties()) {
        var clr = prop.ClrType;
        if (TryGetStronglyTypedIdConverter(clr, baseGeneric, out var converter)) {
          prop.SetValueConverter(converter);
        }
      }
    }
  }

  /// <summary>
  /// Attempts to create a value converter for a type if it inherits from <see cref="StronglyTypedId{TId, TUnderlying}"/>.
  /// </summary>
  /// <param name="clr">The CLR type to check.</param>
  /// <param name="baseGeneric">The base generic type for strongly-typed IDs (<see cref="StronglyTypedId{TId, TUnderlying}"/>).</param>
  /// <param name="converter">When this method returns, contains the value converter if successful; otherwise, null.</param>
  /// <returns><see langword="true"/> if a converter was created; otherwise, <see langword="false"/>.</returns>
  /// <remarks>
  /// This method traverses the type hierarchy to find if the given type is a strongly-typed ID,
  /// and if so, creates an appropriate <see cref="StronglyTypedIdValueConverter{TId, TUnderlying}"/>
  /// for it using reflection to determine the correct generic type arguments.
  /// </remarks>
  private static bool TryGetStronglyTypedIdConverter(Type clr, Type baseGeneric, out ValueConverter? converter) {
    converter = null;
    var current = clr;

    while (current != null) {
      if (current.IsGenericType && current.GetGenericTypeDefinition() == baseGeneric) {
        var args = current.GetGenericArguments();
        var derivedType = clr;
        var underlyingType = args[1];

        var converterType = typeof(StronglyTypedIdValueConverter<,>)
            .MakeGenericType(derivedType, underlyingType);
        converter = (ValueConverter)Activator.CreateInstance(converterType)!;
        return true;
      }
      current = current.BaseType;
    }

    return false;
  }
}
