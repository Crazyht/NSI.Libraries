using System.Globalization;
using NSI.Domains.StrongIdentifier;
using NSI.EntityFramework.Converters;

namespace NSI.EntityFramework.Tests;
public class StronglyTypedIdValueConverterTests {
  [Fact]
  public void Given_GuidBasedId_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var guid = Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture);
    var guidId = new GuidId(guid);
    var guidConverter = new StronglyTypedIdValueConverter<GuidId, Guid>();

    // When
    var dbValue = guidConverter.ConvertToProviderExpression.Compile().Invoke(guidId);
    var modelValue = guidConverter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(guidId.Value, modelValue.Value);
    Assert.IsType<Guid>(dbValue);
  }

  [Fact]
  public void Given_IntBasedId_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var intValue = 42;
    var intId = new IntegerId(intValue);
    var intConverter = new StronglyTypedIdValueConverter<IntegerId, int>();

    // When
    var dbValue = intConverter.ConvertToProviderExpression.Compile().Invoke(intId);
    var modelValue = intConverter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(intId.Value, modelValue.Value);
    Assert.IsType<int>(dbValue);
  }

  [Fact]
  public void Given_StringBasedId_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var stringValue = "test-value";
    var stringId = new StringId(stringValue);
    var stringConverter = new StronglyTypedIdValueConverter<StringId, string>();

    // When
    var dbValue = stringConverter.ConvertToProviderExpression.Compile().Invoke(stringId);
    var modelValue = stringConverter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(stringId.Value, modelValue.Value);
    Assert.IsType<string>(dbValue);
  }
  [Fact]
  public void Given_NullId_When_ConvertingToProvider_Then_ThrowsNullReferenceException() {
    // Given
    GuidId? nullId = null;
    var converter = new StronglyTypedIdValueConverter<GuidId, Guid>();

    // When/Then
    Assert.Throws<NullReferenceException>(() =>
        converter.ConvertToProviderExpression.Compile().Invoke(nullId!));
  }
  [Fact]
  public void Given_IdWithDefaultValue_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var defaultGuid = Guid.Empty;
    var defaultId = new GuidId(defaultGuid);
    var converter = new StronglyTypedIdValueConverter<GuidId, Guid>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(defaultId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(defaultGuid, dbValue);
    Assert.Equal(defaultId.Value, modelValue.Value);
  }

  [Fact]
  public void Given_IdWithCustomValueType_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var customValue = new CustomValueType(42);
    var customId = new CustomTypeId(customValue);
    var converter = new StronglyTypedIdValueConverter<CustomTypeId, CustomValueType>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(customId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(customValue, dbValue);
    Assert.Equal(customId.Value.Value, modelValue.Value.Value);
  }

  [Fact]
  public void Given_DateTimeBasedId_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var dateTime = new DateTime(2025, 5, 3, 12, 30, 45, DateTimeKind.Utc);
    var dateId = new DateId(dateTime);
    var converter = new StronglyTypedIdValueConverter<DateId, DateTime>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(dateId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(dateTime, dbValue);
    Assert.Equal(dateId.Value, modelValue.Value);
    // Ensure no precision loss
    Assert.Equal(dateTime.Kind, dbValue.Kind);
    Assert.Equal(dateTime.Ticks, dbValue.Ticks);
  }
  [Fact]
  public void Given_IdWithMaxValue_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var maxInt = int.MaxValue;
    var maxIntId = new IntegerId(maxInt);
    var converter = new StronglyTypedIdValueConverter<IntegerId, int>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(maxIntId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(int.MaxValue, dbValue);
    Assert.Equal(maxIntId.Value, modelValue.Value);
  }
  [Fact]
  public void Given_IdWithNegativeValue_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var negativeInt = -42;
    var negativeIntId = new IntegerId(negativeInt);
    var converter = new StronglyTypedIdValueConverter<IntegerId, int>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(negativeIntId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(-42, dbValue);
    Assert.Equal(negativeIntId.Value, modelValue.Value);
  }
  [Fact]
  public void Given_IdWithEmptyString_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var emptyString = string.Empty;
    var emptyStringId = new StringId(emptyString);
    var converter = new StronglyTypedIdValueConverter<StringId, string>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(emptyStringId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(string.Empty, dbValue);
    Assert.Equal(emptyStringId.Value, modelValue.Value);
  }
  [Fact]
  public void Given_IdWithLongString_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var longString = new string('a', 10000);
    var longStringId = new StringId(longString);
    var converter = new StronglyTypedIdValueConverter<StringId, string>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(longStringId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(10000, dbValue.Length);
    Assert.Equal(longStringId.Value, modelValue.Value);
  }
  [Fact]
  public void Given_TwoDifferentIdsWithSameValue_When_ConvertingToProvider_Then_DbValuesAreEqual() {
    // Given
    var guid = Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture);
    var id1 = new GuidId(guid);
    var id2 = new GuidId(guid);
    var converter = new StronglyTypedIdValueConverter<GuidId, Guid>();

    // When
    var dbValue1 = converter.ConvertToProviderExpression.Compile().Invoke(id1);
    var dbValue2 = converter.ConvertToProviderExpression.Compile().Invoke(id2);

    // Then
    Assert.Equal(dbValue1, dbValue2);
    // Different instances but equal values
    Assert.NotSame(id1, id2);
    Assert.Equal(id1, id2); // Records with same values should be equal
  }

  [Fact]
  public void Given_DecimalBasedId_When_ConvertingToAndFromProvider_Then_ValueIsPreserved() {
    // Given
    var decimalValue = 123.456m;
    var decimalId = new DecimalId(decimalValue);
    var converter = new StronglyTypedIdValueConverter<DecimalId, decimal>();

    // When
    var dbValue = converter.ConvertToProviderExpression.Compile().Invoke(decimalId);
    var modelValue = converter.ConvertFromProviderExpression.Compile().Invoke(dbValue);

    // Then
    Assert.Equal(123.456m, dbValue);
    Assert.Equal(decimalId.Value, modelValue.Value);
  }

}
internal sealed record DecimalId(decimal Value): StronglyTypedId<DecimalId, decimal>(Value);

internal sealed record DateId(DateTime Value): StronglyTypedId<DateId, DateTime>(Value);

// Add at class level
internal readonly struct CustomValueType(int value): IEquatable<CustomValueType> {
  public int Value { get; } = value;

  public override readonly bool Equals(object? obj) =>
      obj is CustomValueType other && other.Value == Value;

  public readonly bool Equals(CustomValueType other) => Value == other.Value;

  public override readonly int GetHashCode() => Value.GetHashCode();
}

internal sealed record CustomTypeId(CustomValueType Value)
    : StronglyTypedId<CustomTypeId, CustomValueType>(Value);

// Common ID types for testing
internal sealed record IntegerId(int Value): StronglyTypedId<IntegerId, int>(Value);

internal sealed record StringId(string Value): StronglyTypedId<StringId, string>(Value);

internal sealed record GuidId(Guid Value): StronglyTypedId<GuidId, Guid>(Value);
