using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace NSI.Domains.StrongIdentifier;

/// <summary>
/// Provides a generic base for strongly-typed identifiers wrapping a primitive value.
/// </summary>
/// <typeparam name="TId">Concrete identifier type deriving from this base.</typeparam>
/// <typeparam name="TUnderlying">Underlying value type (e.g. <see cref="Guid"/>, <see cref="int"/>, <see cref="long"/>, <see cref="string"/>).</typeparam>
/// <param name="Value">Underlying primitive value of the identifier.</param>
/// <remarks>
/// <para>
/// Hybrid implementation: fast path without reflection for common primitives (Guid, int, long, string)
/// and a cached reflection fallback (TryParse/Parse discovery) for custom value types. Reflection by
/// name is justified here as a last-resort dynamic scenario (generic unknown type) per repository
/// guidelines (see Reflection Standards - last resort rule).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Stable invariant serialization format: <c>TypeName-Value</c>.</description></item>
///   <item><description>Record equality delegates to underlying value.</description></item>
///   <item><description><see cref="TryParse(string?, IFormatProvider?, out TId?)"/> consumes the same format.</description></item>
///   <item><description>Custom underlying types must expose static TryParse / Parse signatures.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>No reflection on hot path for Guid/int/long/string.</description></item>
///   <item><description>Single allocation for serialization (string create).</description></item>
///   <item><description>Reflection metadata resolved once per closed generic type.</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract record StronglyTypedId<TId, TUnderlying>(TUnderlying Value): IStronglyTypedId
  where TId : StronglyTypedId<TId, TUnderlying> {
  private static readonly Type UnderlyingType = typeof(TUnderlying);
  private static readonly string Prefix = typeof(TId).Name + "-";

  // Fast path selector for common primitives (avoids any reflection)
  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Per closed generic optimization; acceptable per guidelines (static cache pattern).")]
  private static readonly bool IsGuid = UnderlyingType == typeof(Guid);
  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Per closed generic optimization; acceptable per guidelines (static cache pattern).")]
  private static readonly bool IsString = UnderlyingType == typeof(string);
  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Per closed generic optimization; acceptable per guidelines (static cache pattern).")]
  private static readonly bool IsInt = UnderlyingType == typeof(int);
  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Per closed generic optimization; acceptable per guidelines (static cache pattern).")]
  private static readonly bool IsLong = UnderlyingType == typeof(long);
  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Per closed generic optimization; acceptable per guidelines (static cache pattern).")]
  private static readonly bool NeedsReflection = !(IsGuid || IsString || IsInt || IsLong);

  // Reflection fallback only initialized when needed
  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Cached only for uncommon underlying types; performance gain outweighs warning.")]
  private static readonly MethodInfo? TryParseWithFormatProviderMethod = NeedsReflection
    ? UnderlyingType.GetMethod(
        "TryParse",
        BindingFlags.Public | BindingFlags.Static,
        null,
        [typeof(string), typeof(IFormatProvider), UnderlyingType.MakeByRefType()],
        null)
    : null;

  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Cached only for uncommon underlying types; performance gain outweighs warning.")]
  private static readonly MethodInfo? TryParseMethod = NeedsReflection
    ? UnderlyingType.GetMethod(
        "TryParse",
        BindingFlags.Public | BindingFlags.Static,
        null,
        [typeof(string), UnderlyingType.MakeByRefType()],
        null)
    : null;

  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Cached only for uncommon underlying types; performance gain outweighs warning.")]
  private static readonly MethodInfo? ParseWithFormatProviderMethod = NeedsReflection
    ? UnderlyingType.GetMethod(
        "Parse",
        BindingFlags.Public | BindingFlags.Static,
        null,
        [typeof(string), typeof(IFormatProvider)],
        null)
    : null;

  [SuppressMessage(
    "Major Code Smell",
    "S2743:Static fields should not be used in generic types",
    Justification = "Cached only for uncommon underlying types; performance gain outweighs warning.")]
  private static readonly MethodInfo? ParseMethod = NeedsReflection
    ? UnderlyingType.GetMethod(
        "Parse",
        BindingFlags.Public | BindingFlags.Static,
        null,
        [typeof(string)],
        null)
    : null;

  /// <summary>
  /// Attempts to parse a serialized identifier produced by <see cref="ToSerializedString(TId)"/>.
  /// </summary>
  /// <param name="input">Serialized form: <c>TypeName-Value</c>.</param>
  /// <param name="provider">Optional culture provider for underlying parsing.</param>
  /// <param name="result">Resulting identifier on success; otherwise null.</param>
  /// <returns><c>true</c> if parsing succeeds; otherwise <c>false</c>.</returns>
  [SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Required for ASP.NET Core model binding & general discoverability.")]
  public static bool TryParse(string? input, IFormatProvider? provider, out TId? result) {
    result = null;
    if (!IsValidInput(input, out var payload)) {
      return false;
    }

    if (!TryParseUnderlying(payload, provider, out var parsed)) {
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
    if (!input.StartsWith(Prefix, StringComparison.Ordinal)) {
      return false;
    }
    payload = input[Prefix.Length..];
    return true;
  }

  private static bool TryParseUnderlying(string payload, IFormatProvider? provider, out object? parsed) {
    parsed = null;
    if (TryParsePrimitive(payload, provider, out parsed)) {
      return true;
    }
    if (!NeedsReflection) {
      return false;
    }
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
    throw new NotSupportedException($"Underlying type '{UnderlyingType.Name}' exposes no supported Parse / TryParse methods.");
  }

  private static bool TryParsePrimitive(string payload, IFormatProvider? provider, out object? value) {
    value = null;
    if (IsGuid) {
      if (Guid.TryParse(payload, out var g)) {
        value = g;
        return true;
      }
      return false;
    }
    if (IsInt) {
      var culture = provider as CultureInfo ?? CultureInfo.InvariantCulture;
      if (int.TryParse(payload, NumberStyles.Integer, culture, out var i)) {
        value = i;
        return true;
      }
      return false;
    }
    if (IsLong) {
      var culture = provider as CultureInfo ?? CultureInfo.InvariantCulture;
      if (long.TryParse(payload, NumberStyles.Integer, culture, out var l)) {
        value = l;
        return true;
      }
      return false;
    }
    if (IsString) {
      value = payload;
      return true;
    }
    return false;
  }

  private static bool InvokeTryParseWithFormatProvider(string payload, IFormatProvider? provider, out object? parsed) {
    const int ParsedValueIndex = 2;
    var args = new object?[] { payload, provider, Activator.CreateInstance(UnderlyingType) };
    var success = (bool)TryParseWithFormatProviderMethod!.Invoke(null, args)!;
    parsed = success ? args[ParsedValueIndex] : null;
    return success;
  }

  private static bool InvokeTryParse(string payload, out object? parsed) {
    const int ParsedValueIndex = 1;
    var args = new object?[] { payload, Activator.CreateInstance(UnderlyingType) };
    var success = (bool)TryParseMethod!.Invoke(null, args)!;
    parsed = success ? args[ParsedValueIndex] : null;
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

  /// <summary>Serializes an identifier to the canonical <c>TypeName-Value</c> form.</summary>
  /// <param name="id">Identifier instance (non-null).</param>
  /// <returns>Invariant string representation.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="id"/> is null.</exception>
  /// <remarks>Uses invariant culture; safe for persistence, logging and diagnostics.</remarks>
  [SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Utility required by JSON / EF converters & external callers.")]
  public static string ToSerializedString(TId id) {
    ArgumentNullException.ThrowIfNull(id);
    return id.Value is IFormattable f
      ? string.Create(CultureInfo.InvariantCulture, $"{typeof(TId).Name}-{f.ToString(null, CultureInfo.InvariantCulture)}")
      : string.Create(CultureInfo.InvariantCulture, $"{typeof(TId).Name}-{id.Value}");
  }

  /// <summary>Returns the canonical string form of this identifier.</summary>
  /// <returns>Invariant string representation.</returns>
  public override string ToString() => ToSerializedString((TId)this);
}
