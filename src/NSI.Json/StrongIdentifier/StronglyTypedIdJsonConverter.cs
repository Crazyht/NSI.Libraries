using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Json.StrongIdentifier;

/// <summary>
/// JSON converter for a concrete strongly-typed identifier (<see cref="StronglyTypedId{TId, TUnderlying}"/>).
/// </summary>
/// <typeparam name="TId">Concrete strongly-typed identifier type.</typeparam>
/// <typeparam name="TUnderlying">Underlying primitive type (e.g. <see cref="Guid"/>, <see cref="int"/>, <see cref="string"/>).</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Serialization format: <c>TypeName-UnderlyingValue</c>.</description></item>
///   <item><description>Null JSON token maps to <c>null</c> reference (when allowed by consumer).</description></item>
///   <item><description>Deserialization enforces correct type prefix via <see cref="StronglyTypedId{TId, TUnderlying}.TryParse"/>.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Do not register manually; prefer the factory <see cref="StronglyTypedIdJsonConverterFactory"/>.</description></item>
///   <item><description>Keep identifier types minimal (single value) for predictable JSON contracts.</description></item>
///   <item><description>Use invariant culture for deterministic round‑trip of numeric / GUID values.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>No allocations beyond the produced JSON string (writer handles escaping).</description></item>
///   <item><description>Parsing delegates to optimized static <c>TryParse</c> logic.</description></item>
///   <item><description>Branching limited to token type validation + parse success path.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Converter instances are stateless and reusable across threads.</para>
/// </remarks>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions {
///   Converters = { new StronglyTypedIdJsonConverterFactory() }
/// };
/// var json = JsonSerializer.Serialize(UserId.New(), options);
/// var id = JsonSerializer.Deserialize&lt;UserId&gt;(json, options);
/// </code>
/// </example>
/// <seealso cref="StronglyTypedIdJsonConverterFactory"/>
/// <seealso cref="StronglyTypedId{TId, TUnderlying}"/>
public class StronglyTypedIdJsonConverter<TId, TUnderlying>: JsonConverter<TId>
  where TId : StronglyTypedId<TId, TUnderlying> {
  /// <summary>
  /// Reads a strongly-typed identifier from its serialized string form.
  /// </summary>
  /// <param name="reader">JSON reader (by ref).</param>
  /// <param name="typeToConvert">Target CLR type (ignored; enforced by generic constraint).</param>
  /// <param name="options">Serializer options (unused).</param>
  /// <returns>Parsed strongly-typed identifier or <c>null</c>.</returns>
  /// <exception cref="JsonException">Invalid token kind or parse failure.</exception>
  public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    if (reader.TokenType == JsonTokenType.Null) {
      return null; // propagate null
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
  /// Writes the strongly-typed identifier using its canonical serialized representation.
  /// </summary>
  /// <param name="writer">JSON writer (non-null).</param>
  /// <param name="value">Value to serialize (non-null).</param>
  /// <param name="options">Serializer options (unused).</param>
  public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) {
    ArgumentNullException.ThrowIfNull(writer);
    if (value is null) { // Defensive: generic constraint typically ensures non-null struct underlying
      writer.WriteNullValue();
      return;
    }
    writer.WriteStringValue(StronglyTypedId<TId, TUnderlying>.ToSerializedString(value));
  }
}
