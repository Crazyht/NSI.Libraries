using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NSI.Domains.StrongIdentifier;

namespace NSI.EntityFramework.Converters {
  /// <summary>
  /// Value converter for Entity Framework Core that enables the persistence of strongly-typed IDs.
  /// </summary>
  /// <typeparam name="TId">The strongly-typed ID type to convert.</typeparam>
  /// <typeparam name="TUnderlying">The underlying primitive type stored in the database (e.g., <see cref="Guid"/>, <see cref="int"/>, <see cref="string"/>).</typeparam>
  /// <remarks>
  /// <para>
  /// This converter transparently translates between domain-specific strongly-typed IDs and their 
  /// underlying primitive values when reading from or writing to the database. It allows Entity Framework
  /// to store the underlying primitive value in the database while maintaining the type safety of 
  /// strongly-typed IDs in your domain model.
  /// </para>
  /// <para>
  /// The converter is automatically applied to all properties using strongly-typed IDs when using
  /// the <see cref="StronglyTypedIdEfCoreExtensions.ApplyStronglyTypedIdConversions"/> extension method
  /// on your model configuration.
  /// </para>
  /// </remarks>
  /// <example>
  /// Applying converters in your DbContext:
  /// <code>
  /// protected override void OnModelCreating(ModelBuilder modelBuilder)
  /// {
  ///     base.OnModelCreating(modelBuilder);
  ///     
  ///     // Apply converters for all strongly-typed IDs
  ///     modelBuilder.ApplyStronglyTypedIdConversions();
  ///     
  ///     // Or manually for a specific property
  ///     modelBuilder.Entity&lt;User&gt;()
  ///         .Property(e => e.Id)
  ///         .HasConversion(new StronglyTypedIdValueConverter&lt;UserId, Guid&gt;());
  /// }
  /// </code>
  /// </example>
  public class StronglyTypedIdValueConverter<TId, TUnderlying>
  : ValueConverter<TId, TUnderlying>
  where TId : StronglyTypedId<TId, TUnderlying> {
    /// <summary>
    /// Initializes a new instance of the <see cref="StronglyTypedIdValueConverter{TId, TUnderlying}"/> class.
    /// </summary>
    /// <remarks>
    /// The converter is configured with two conversion functions:
    /// <list type="bullet">
    ///   <item><description>To database: Extracts the underlying value from the strongly-typed ID</description></item>
    ///   <item><description>From database: Creates a new strongly-typed ID instance from the primitive value</description></item>
    /// </list>
    /// </remarks>
    public StronglyTypedIdValueConverter()
        : base(
            id => id.Value,
            value => (TId)Activator.CreateInstance(typeof(TId), value)!) { }
  }
}
