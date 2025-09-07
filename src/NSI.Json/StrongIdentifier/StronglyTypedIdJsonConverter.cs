using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Json.StrongIdentifier;
/// <summary>
/// JSON converter for serializing and deserializing a specific strongly-typed ID type.
/// </summary>
/// <typeparam name="TId">The concrete strongly-typed ID type to convert.</typeparam>
/// <typeparam name="TUnderlying">The underlying primitive type (e.g., <see cref="Guid"/>, <see cref="int"/>, <see cref="string"/>) stored in the ID.</typeparam>
/// <remarks>
/// <para>
/// This converter is responsible for the actual conversion between strongly-typed IDs and their
/// JSON string representation. It uses the specialized string format "TypeName-Value" for serialization
/// to ensure type safety during JSON deserialization.
/// </para>
/// <para>
/// This converter is not typically used directly; instead, the <see cref="StronglyTypedIdJsonConverterFactory"/>
/// automatically creates and uses instances of this converter for strongly-typed ID properties as needed.
/// </para>
/// </remarks>
/// <seealso cref="StronglyTypedIdJsonConverterFactory"/>
/// <seealso cref="StronglyTypedId{TId, TUnderlying}"/>
public class StronglyTypedIdJsonConverter<TId, TUnderlying>: JsonConverter<TId>
  where TId : StronglyTypedId<TId, TUnderlying> {
  /// <summary>
  /// Reads and converts the JSON to a strongly-typed ID.
  /// </summary>
  /// <param name="reader">The UTF-8 JSON reader to read from.</param>
  /// <param name="typeToConvert">The type of object to convert.</param>
  /// <param name="options">Options to control the behavior during reading.</param>
  /// <returns>The converted strongly-typed ID.</returns>
  /// <exception cref="JsonException">Thrown when the JSON is in an unexpected format or the string cannot be parsed into a valid ID.</exception>
  /// <remarks>
  /// <para>
  /// This method expects the JSON to contain a string in the format "TypeName-Value" where TypeName
  /// matches the name of the ID type being deserialized. This ensures that IDs cannot be mistakenly
  /// assigned to the wrong type during deserialization.
  /// </para>
  /// <para>
  /// The <see cref="StronglyTypedId{TId, TUnderlying}.TryParse"/> method is used internally to convert
  /// the string representation back to the strongly-typed ID.
  /// </para>
  /// </remarks>
  public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    if (reader.TokenType == JsonTokenType.Null) {
      return null;
    }

    if (reader.TokenType != JsonTokenType.String) {
      throw new JsonException($"Expected string token, but got {reader.TokenType}");
    }

    var raw = reader.GetString();
    if (StronglyTypedId<TId, TUnderlying>.TryParse(raw, CultureInfo.InvariantCulture, out var id)) {
      return id;
    }

    throw new JsonException($"Invalid serialized {typeof(TId).Name}: '{raw}'");
  }

  /// <summary>
  /// Writes a strongly-typed ID as JSON.
  /// </summary>
  /// <param name="writer">The UTF-8 JSON writer to write to.</param>
  /// <param name="value">The value to convert.</param>
  /// <param name="options">Options to control the behavior during writing.</param>
  /// <remarks>
  /// <para>
  /// This method serializes the strongly-typed ID as a string in the format "TypeName-Value",
  /// using the <see cref="StronglyTypedId{TId, TUnderlying}.ToSerializedString"/> method.
  /// </para>
  /// <para>
  /// This format preserves the type information along with the underlying value, allowing for
  /// type-safe deserialization even when multiple ID types share the same underlying primitive type.
  /// </para>
  /// </remarks>
  public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) {
    ArgumentNullException.ThrowIfNull(writer);
    if (value is null) {
      writer.WriteNullValue();
      return;
    }
    writer.WriteStringValue(StronglyTypedId<TId, TUnderlying>.ToSerializedString(value));
  }
}
