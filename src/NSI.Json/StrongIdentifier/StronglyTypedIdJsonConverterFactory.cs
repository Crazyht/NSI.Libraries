using System.Text.Json;
using System.Text.Json.Serialization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Json.StrongIdentifier {
  /// <summary>
  /// JSON converter factory for serializing and deserializing strongly-typed IDs.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This factory enables System.Text.Json to handle strongly-typed IDs by detecting types that 
  /// inherit from <see cref="StronglyTypedId{TId, TUnderlying}"/> and dynamically creating 
  /// appropriate converters for them.
  /// </para>
  /// <para>
  /// Register this factory with JsonSerializerOptions to enable automatic JSON serialization
  /// of strongly-typed IDs:
  /// <code>
  /// var options = new JsonSerializerOptions { 
  ///     Converters = { new StronglyTypedIdJsonConverterFactory() } 
  /// };
  /// </code>
  /// </para>
  /// <para>
  /// When serialized, IDs will be represented as strings in the format "TypeName-Value",
  /// which maintains type safety during serialization/deserialization and improves readability.
  /// </para>
  /// </remarks>
  /// <example>
  /// Serializing and deserializing an entity with strongly-typed ID:
  /// <code>
  /// // Setup serializer options
  /// var options = new JsonSerializerOptions { 
  ///     Converters = { new StronglyTypedIdJsonConverterFactory() } 
  /// };
  /// 
  /// // Serialize entity with strongly-typed ID
  /// var user = new User { Id = new UserId(Guid.NewGuid()) };
  /// string json = JsonSerializer.Serialize(user, options);
  /// // Result: {"Id":"UserId-01234567-89ab-cdef-0123-456789abcdef"}
  /// 
  /// // Deserialize back to strongly-typed ID
  /// var deserialized = JsonSerializer.Deserialize&lt;User&gt;(json, options);
  /// </code>
  /// </example>
  public class StronglyTypedIdJsonConverterFactory: JsonConverterFactory {
    /// <summary>
    /// Determines whether the converter can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to check if this converter can handle.</param>
    /// <returns>
    /// <see langword="true"/> if the type inherits from <see cref="StronglyTypedId{TId, TUnderlying}"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method traverses the inheritance hierarchy to check if the type derives from 
    /// <see cref="StronglyTypedId{TId, TUnderlying}"/> at any level.
    /// </remarks>
    public override bool CanConvert(Type typeToConvert) {
      var t = typeToConvert;
      while (t != null) {
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(StronglyTypedId<,>)) {
          return true;
        }

        t = t.BaseType;
      }
      return false;
    }

    /// <summary>
    /// Creates a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to create a converter for.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>A converter that can process the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="typeToConvert"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown if the type is not a StronglyTypedId.</exception>
    /// <remarks>
    /// This method dynamically creates an instance of <see cref="StronglyTypedIdJsonConverter{TId, TUnderlying}"/>
    /// with the appropriate generic type arguments for the requested type.
    /// </remarks>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
      ArgumentNullException.ThrowIfNull(typeToConvert);
      if (!CanConvert(typeToConvert)) {
        throw new NotSupportedException($"Type {typeToConvert.Name} is not a StronglyTypedId");
      }
      var baseType = typeToConvert;
      while (!(baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(StronglyTypedId<,>))) {
        baseType = baseType.BaseType!;
      }

      var args = baseType.GetGenericArguments();
      var converterType = typeof(StronglyTypedIdJsonConverter<,>)
          .MakeGenericType(typeToConvert, args[1]);

      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
  }
}
