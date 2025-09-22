using System.Reflection;

namespace NSI.Core.Common;

/// <summary>
/// Reflection helper extensions for <see cref="MemberInfo"/> providing efficient, allocation-free
/// metadata access utilities used across expression / specification infrastructure.
/// </summary>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Provides unified member type resolution for fields and properties.</description></item>
///   <item><description>Falls back to <see cref="object"/> for unsupported member kinds (events, methods).</description></item>
///   <item><description>Designed for hot-path usage during expression tree construction.</description></item>
/// </list>
/// </para>
/// <para>Performance: Single switch dispatch on runtime member type; no caching required since
/// <see cref="MemberInfo"/> instances are stable and resolution cost is trivial.</para>
/// <para>Thread-safety: Pure extension method – fully thread-safe.</para>
/// </remarks>
public static class MemberInfoExtensions {
  /// <summary>
  /// Gets the declared CLR type for a field or property member.
  /// </summary>
  /// <param name="member">Member to inspect (non-null).</param>
  /// <returns>The member type for properties/fields; otherwise <see cref="object"/>.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="member"/> is null.</exception>
  public static Type GetMemberType(this MemberInfo member) {
    ArgumentNullException.ThrowIfNull(member);
    return member switch {
      PropertyInfo p => p.PropertyType,
      FieldInfo f => f.FieldType,
      _ => typeof(object)
    };
  }
}
