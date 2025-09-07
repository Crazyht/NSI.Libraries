using System.Globalization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Domains;

/// <summary>
/// Strongly typed identifier for a user aggregate.
/// </summary>
/// <remarks>
/// <para>Provides type safety over raw <see cref="Guid"/> usage and custom formatting helpers.</para>
/// </remarks>
public sealed record UserId(Guid Value): StronglyTypedId<UserId, Guid>(Value) {
  /// <summary>Returns the serialized string representation (overrides record default).</summary>
  public override string ToString() => ToSerializedString(this);

  /// <summary>Gets an empty (all zero) user identifier.</summary>
  public static UserId Empty => new(Guid.Empty);

  /// <summary>Gets the reserved system user identifier.</summary>
  public static UserId SystemUserId => new(Guid.Parse("00000000-0000-0000-0000-000000000001", CultureInfo.InvariantCulture));
}
