using System.Text.Json;
using System.Text.Json.Serialization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Json.StrongIdentifier;
/// <summary>
/// JSON converter factory enabling serialization of strongly-typed identifiers
/// (<see cref="StronglyTypedId{TId, TUnderlying}"/>) via System.Text.Json.
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Detects types deriving (directly or indirectly) from <see cref="StronglyTypedId{TId, TUnderlying}"/>.</description></item>
///   <item><description>Creates closed generic <see cref="StronglyTypedIdJsonConverter{TId, TUnderlying}"/> instances.</description></item>
///   <item><description>System.Text.Json caches produced converters per target type (factory is stateless).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Register once: <c>options.Converters.Add(new StronglyTypedIdJsonConverterFactory())</c>.</description></item>
///   <item><description>Do not manually register individual strongly-typed id converters.</description></item>
///   <item><description>Keep identifier constructors stable for deterministic deserialization.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Reflection limited to inheritance walk + generic argument extraction per id type.</description></item>
///   <item><description>Serializer-level caching prevents repeated factory invocation for same CLR type.</description></item>
///   <item><description>No additional allocations beyond the converter instance itself.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Factory is stateless and safe for concurrent use.</para>
/// <para>Error handling: Non StronglyTypedId types passed to <see cref="CreateConverter"/> raise <see cref="NotSupportedException"/>.</para>
/// </remarks>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions {
///   Converters = { new StronglyTypedIdJsonConverterFactory() }
/// };
/// var user = new User { Id = UserId.New() };
/// var json = JsonSerializer.Serialize(user, options);
/// var roundTrip = JsonSerializer.Deserialize&lt;User&gt;(json, options);
/// </code>
/// </example>
public class StronglyTypedIdJsonConverterFactory: JsonConverterFactory {
  /// <summary>
  /// Determines whether the provided type can be handled (derives from strongly-typed id base).
  /// </summary>
  /// <param name="typeToConvert">Candidate CLR type.</param>
  /// <returns><c>true</c> if supported; otherwise <c>false</c>.</returns>
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
  /// Creates a concrete JSON converter for the specified strongly-typed identifier.
  /// </summary>
  /// <param name="typeToConvert">Concrete strongly-typed id type.</param>
  /// <param name="options">Serializer options (ignored).</param>
  /// <returns>Instance of a closed <see cref="StronglyTypedIdJsonConverter{TId, TUnderlying}"/>.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="typeToConvert"/> is null.</exception>
  /// <exception cref="NotSupportedException">When <paramref name="typeToConvert"/> is not a strongly-typed id.</exception>
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
    ArgumentNullException.ThrowIfNull(typeToConvert);
    if (!CanConvert(typeToConvert)) {
      throw new NotSupportedException($"Type {typeToConvert.Name} is not a StronglyTypedId");
    }

    var baseType = typeToConvert;
    while (!(baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(StronglyTypedId<,>))) {
      baseType = baseType.BaseType!; // Safe: loop guarded by CanConvert earlier
    }

    var args = baseType.GetGenericArguments(); // [TId, TUnderlying]
    var converterType = typeof(StronglyTypedIdJsonConverter<,>).MakeGenericType(typeToConvert, args[1]);
    return (JsonConverter)Activator.CreateInstance(converterType)!;
  }
}
