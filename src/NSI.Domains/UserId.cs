using System.Globalization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Domains {
  public sealed record UserId(Guid Value): StronglyTypedId<UserId, Guid>(Value) {
    // Override record-generated ToString to use prefixed format
    public override string ToString() => ToSerializedString(this);

    public static UserId Empty => new(Guid.Empty);

    public static UserId SystemUserId => new(Guid.Parse("00000000-0000-0000-0000-000000000001", CultureInfo.InvariantCulture));
  }
}
