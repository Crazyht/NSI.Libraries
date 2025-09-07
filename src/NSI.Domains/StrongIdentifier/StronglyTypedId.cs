using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace NSI.Domains.StrongIdentifier {

  /// <summary>
  /// Base record for implementing strongly-typed identifiers that wrap primitive types.
  /// </summary>
  /// <typeparam name="TId">The concrete ID type that derives from this class.</typeparam>
  /// <typeparam name="TUnderlying">The underlying primitive type (e.g., <see cref="Guid"/>, <see cref="int"/>, <see cref="string"/>) stored in the ID.</typeparam>
  /// <remarks>
  /// <para>
  /// This class implements the strongly-typed ID pattern to avoid primitive obsession with IDs.
  /// It provides type safety by wrapping primitive values in domain-specific types,
  /// preventing IDs of different entity types from being accidentally mixed.
  /// </para>
  /// <para>
  /// Features include:
  /// <list type="bullet">
  ///   <item><description>Type-safe IDs that cannot be accidentally mixed across different entity types</description></item>
  ///   <item><description>String representation with type name prefix for improved debugging and logging</description></item>
  ///   <item><description>Automatic parsing from strings to strongly-typed IDs via reflection</description></item>
  ///   <item><description>Support for Entity Framework Core via <c>NSI.EntityFramework.Converters.StronglyTypedIdValueConverter{TId, TUnderlying}</c></description></item>
  ///   <item><description>JSON serialization support via <c>NSI.Json.StronglyTypedIdJsonConverterFactory</c></description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// Creating a strongly-typed ID:
  /// <code>
  /// public sealed record UserId(Guid Value) : StronglyTypedId&lt;UserId, Guid>(Value);
  /// 
  /// // Usage
  /// var id = new UserId(Guid.NewGuid());
  /// </code>
  /// </example>
  public abstract record StronglyTypedId<TId, TUnderlying>(TUnderlying Value): IStronglyTypedId
      where TId : StronglyTypedId<TId, TUnderlying> {
    // Cache reflection info once per closed generic type
    private static readonly Type UnderlyingType = typeof(TUnderlying);

    [SuppressMessage(
      "Major Code Smell",
      "S2743:Static fields should not be used in generic types",
      Justification = "Reflection cache on Generic Type.")]
    private static readonly MethodInfo? TryParseWithFormatProviderMethod = UnderlyingType.GetMethod(
            "TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), typeof(IFormatProvider), UnderlyingType.MakeByRefType()],
            null);

    [SuppressMessage(
      "Major Code Smell",
      "S2743:Static fields should not be used in generic types",
      Justification = "Reflection cache on Generic Type.")]
    private static readonly MethodInfo? TryParseMethod = UnderlyingType.GetMethod(
            "TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), UnderlyingType.MakeByRefType()],
            null);

    [SuppressMessage(
      "Major Code Smell",
      "S2743:Static fields should not be used in generic types",
      Justification = "Reflection cache on Generic Type.")]
    private static readonly MethodInfo? ParseWithFormatProviderMethod = UnderlyingType.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), typeof(IFormatProvider)],
            null);

    [SuppressMessage(
      "Major Code Smell",
      "S2743:Static fields should not be used in generic types",
      Justification = "Reflection cache on Generic Type.")]
    private static readonly MethodInfo? ParseMethod = UnderlyingType.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string)],
            null);

    /// <summary>
    /// Attempts to parse a string representation back into a strongly-typed ID.
    /// </summary>
    /// <param name="input">The string to parse, which should be in the format "TypeName-Value".</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="result">When this method returns, contains the parsed ID value if successful; otherwise, null.</param>
    /// <returns><see langword="true"/> if the string was successfully parsed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method handles parsing string representations created by <see cref="ToSerializedString"/> and expects
    /// the input string to be in the format "{TypeName}-{Value}" where TypeName is the name of the concrete ID type.
    /// </remarks>
    [SuppressMessage(
      "Design",
      "CA1000:Do not declare static members on generic types",
      Justification = "Needed by AspNet Core for Route & Query Binding")]
    public static bool TryParse(string? input, IFormatProvider? provider, out TId? result) {
      result = null;

      if (!IsValidInput(input, out var payload)) {
        return false;
      }

      if (!TryParsePayload(payload, provider, out var parsed)) {
        return false;
      }

      result = (TId?)Activator.CreateInstance(typeof(TId), parsed);
      return result != null;
    }

    private static bool IsValidInput(string? input, out string payload) {
      payload = string.Empty;

      if (string.IsNullOrEmpty(input)) {
        return false;
      }

      var prefix = typeof(TId).Name + "-";
      if (!input.StartsWith(prefix, StringComparison.Ordinal)) {
        return false;
      }

      payload = input[prefix.Length..];
      return true;
    }

    private static bool TryParsePayload(string payload, IFormatProvider? provider, out object? parsed) {
      parsed = null;

      if (TryParseWithFormatProviderMethod != null) {
        return InvokeTryParseWithFormatProvider(payload, provider, out parsed);
      }

      if (TryParseMethod != null) {
        return InvokeTryParse(payload, out parsed);
      }

      if (ParseWithFormatProviderMethod != null) {
        return InvokeParseWithFormatProvider(payload, provider, out parsed);
      }

      if (ParseMethod != null) {
        return InvokeParse(payload, out parsed);
      }

      if (UnderlyingType == typeof(string)) {
        parsed = payload;
        return true;
      }

      throw new NotSupportedException($"No TryParse or Parse on {UnderlyingType.Name}");
    }

    private static bool InvokeTryParseWithFormatProvider(string payload, IFormatProvider? provider, out object? parsed) {
      const int ParsedValueIndex = 2; // Assign magic number '2' to a well-named constant
      var args = new object?[] { payload, provider, Activator.CreateInstance(UnderlyingType) };
      var success = (bool)TryParseWithFormatProviderMethod!.Invoke(null, args)!;
      parsed = success && args[ParsedValueIndex] != null ? args[ParsedValueIndex]! : null; // Use the constant instead of the magic number
      return success;
    }

    private static bool InvokeTryParse(string payload, out object? parsed) {
      const int ParsedValueIndex = 1; // Assign magic number '1' to a well-named constant
      var args = new object?[] { payload, Activator.CreateInstance(UnderlyingType) };
      var success = (bool)TryParseMethod!.Invoke(null, args)!;
      parsed = success && args[ParsedValueIndex] != null ? args[ParsedValueIndex]! : null; // Use the constant instead of the magic number
      return success;
    }

    private static bool InvokeParseWithFormatProvider(string payload, IFormatProvider? provider, out object? parsed) {
      try {
        parsed = ParseWithFormatProviderMethod!.Invoke(null, [payload, provider])!;
        return true;
      } catch (TargetInvocationException ex) when (ex.InnerException is FormatException) {
        parsed = null;
        return false;
      }
    }

    private static bool InvokeParse(string payload, out object? parsed) {
      try {
        parsed = ParseMethod!.Invoke(null, [payload])!;
        return true;
      } catch (TargetInvocationException ex) when (ex.InnerException is FormatException) {
        parsed = null;
        return false;
      }
    }

    /// <summary>
    /// Converts the strongly-typed ID to its string representation for serialization.
    /// </summary>
    /// <param name="id">The strongly-typed ID to convert.</param>
    /// <returns>A string representation in the format "TypeName-Value".</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
    /// <remarks>
    /// The string format is designed to be human-readable and consists of the type name 
    /// followed by a hyphen and then the underlying value. This format makes debugging easier
    /// and prevents confusion between different ID types with the same underlying value.
    /// </remarks>
    [SuppressMessage(
      "Design",
      "CA1000:Do not declare static members on generic types",
      Justification = "Needed by JsonConverter")]
    public static string ToSerializedString(TId id) {
      ArgumentNullException.ThrowIfNull(id);
      return $"{typeof(TId).Name}-{(id.Value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : id.Value?.ToString())}";
    }

    /// <summary>
    /// Returns a string representation of this strongly-typed ID.
    /// </summary>
    /// <returns>A string in the format "TypeName-Value".</returns>
    /// <remarks>
    /// Override of the <see cref="object.ToString"/> method that uses <see cref="ToSerializedString"/> 
    /// to create a consistent string representation with the type name as prefix.
    /// </remarks>
    public override string ToString() => ToSerializedString((TId)this);
  }
}
