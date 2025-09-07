using System.Globalization;
using NSI.Domains.StrongIdentifier;

namespace NSI.Domains.Tests {
  public class StronglyTypedIdTests {
    [Theory]
    [InlineData("UserId-00000000-0000-0000-0000-000000000000", true)]
    [InlineData("UserId-11111111-1111-1111-1111-111111111111", true)]
    [InlineData("Wrong-11111111-1111-1111-1111-111111111111", false)]
    [InlineData("UserId-invalid-guid", false)]
    public void Given_FormattedString_When_TryParse_Then_ReturnsExpectedResult(string input, bool expected) {
      // Given - input from parameter

      // When
      var success = StronglyTypedId<UserId, Guid>.TryParse(input, CultureInfo.InvariantCulture, out var result);

      // Then
      Assert.Equal(expected, success);
      if (success) {
        Assert.NotNull(result);
        Assert.IsType<UserId>(result);
      } else {
        Assert.Null(result);
      }
    }

    [Fact]
    public static void Given_StronglyTypedId_When_ToString_Then_ReturnsPrefixedValue() {
      // Given
      var guid = Guid.NewGuid();
      var id = new UserId(guid);

      // When
      var str = id.ToString();

      // Then
      Assert.StartsWith("UserId-", str, StringComparison.InvariantCulture);
      Assert.EndsWith(guid.ToString("D", CultureInfo.InvariantCulture), str, StringComparison.InvariantCulture);
    }

    // Additional test cases start here

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Given_NullOrEmptyInput_When_TryParse_Then_ReturnsFalse(string? input, bool expected) {
      // Given - input from parameter

      // When
      var success = StronglyTypedId<UserId, Guid>.TryParse(input, CultureInfo.InvariantCulture, out var result);

      // Then
      Assert.Equal(expected, success);
      Assert.Null(result);
    }

    [Fact]
    public static void Given_StronglyTypedId_When_ToSerializedString_Then_ReturnsPrefixedValue() {
      // Given
      var guid = Guid.NewGuid();
      var id = new UserId(guid);

      // When
      var str = StronglyTypedId<UserId, Guid>.ToSerializedString(id);

      // Then
      Assert.StartsWith("UserId-", str, StringComparison.InvariantCulture);
      Assert.EndsWith(guid.ToString("D", CultureInfo.InvariantCulture), str, StringComparison.InvariantCulture);
    }

    [Fact]
    public void Given_NullId_When_ToSerializedString_Then_ThrowsArgumentNullException() {
      // Given
      UserId? id = null;

      // When/Then
      Assert.Throws<ArgumentNullException>(() => StronglyTypedId<UserId, Guid>.ToSerializedString(id!));
    }

    [Fact]
    public static void Given_StringBasedId_When_ToString_Then_ReturnsPrefixedValue() {
      const string? value = "test-value";
      var id = new StringId(value);

      // When
      var str = id.ToString();

      // Then
      Assert.StartsWith("StringId-", str, StringComparison.InvariantCulture);
      Assert.EndsWith(value, str, StringComparison.InvariantCulture);
    }

    [Fact]
    public static void Given_IntBasedId_When_ToString_Then_ReturnsPrefixedValue() {
      const int value = 42;
      var id = new IntegerId(value);

      // When
      var str = id.ToString();

      // Then
      Assert.StartsWith("IntegerId-", str, StringComparison.InvariantCulture);
      Assert.EndsWith("42", str, StringComparison.InvariantCulture);
    }

    [Theory]
    [InlineData("StringId-test-value", "test-value", true)]
    [InlineData("StringId-", "", true)]
    [InlineData("WrongId-test-value", null, false)]
    public void Given_StringIdFormat_When_TryParse_Then_ReturnsExpectedResult(
        string input, string? expectedValue, bool expectedSuccess) {
      // Given - input from parameter

      // When
      var success = StronglyTypedId<StringId, string>.TryParse(input, CultureInfo.InvariantCulture, out var result);

      // Then
      Assert.Equal(expectedSuccess, success);
      if (success) {
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
      } else {
        Assert.Null(result);
      }
    }

    [Theory]
    [InlineData("IntegerId-42", 42, true)]
    [InlineData("IntegerId--123", -123, true)]
    [InlineData("IntegerId-0", 0, true)]
    [InlineData("IntegerId-NotANumber", null, false)]
    [InlineData("WrongId-42", null, false)]
    public void Given_IntIdFormat_When_TryParse_Then_ReturnsExpectedResult(
        string input, int? expectedValue, bool expectedSuccess) {
      // Given - input from parameter

      // When
      var success = StronglyTypedId<IntegerId, int>.TryParse(input, CultureInfo.InvariantCulture, out var result);

      // Then
      Assert.Equal(expectedSuccess, success);
      if (success) {
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
      } else {
        Assert.Null(result);
      }
    }

    [Fact]
    public void Given_UnsupportedUnderlyingType_When_TryParse_Then_ThrowsNotSupportedException() {
      const string? input = "CustomTypeId-value";

      // When/Then
      Assert.Throws<NotSupportedException>(() =>
          StronglyTypedId<CustomTypeId, CustomType>.TryParse(input, CultureInfo.InvariantCulture, out _));
    }

    [Fact]
    public void Given_GuidIdWithToStringFormat_When_RoundTrip_Then_OriginalValueIsPreserved() {
      // Given
      var originalGuid = Guid.NewGuid();
      var originalId = new UserId(originalGuid);

      // When - Round trip through string serialization
      var serialized = originalId.ToString();
      var success = StronglyTypedId<UserId, Guid>.TryParse(serialized, CultureInfo.InvariantCulture, out var deserializedId);

      // Then
      Assert.True(success);
      Assert.NotNull(deserializedId);
      Assert.Equal(originalGuid, deserializedId.Value);
    }

    [Fact]
    public void Given_StringIdWithHyphens_When_ToString_Then_FormatsCorrectly() {
      const string? value = "test-with-hyphens";
      var id = new StringId(value);

      // When
      var str = id.ToString();

      // Then
      Assert.Equal("StringId-test-with-hyphens", str);
    }

    [Fact]
    public void Given_DateTimeBasedId_When_ToString_Then_FormatsPrecisely() {
      var date = new DateTime(2025, 5, 3, 12, 0, 0, DateTimeKind.Utc);
      var id = new DateTimeId(date);

      var str = id.ToString();

      // Verify format precision is maintained
      Assert.StartsWith("DateTimeId-", str, StringComparison.InvariantCulture);
      Assert.EndsWith($"-{date.ToString(CultureInfo.InvariantCulture)}", str, StringComparison.InvariantCulture);

      // Verify it can be parsed back correctly
      var success = StronglyTypedId<DateTimeId, DateTime>.TryParse(str, CultureInfo.InvariantCulture, out var parsedId);
      Assert.True(success);
      Assert.Equal(date, parsedId!.Value);
    }
    [Fact]
    public void Given_TypeWithTryParseWithoutFormatProvider_When_TryParse_Then_UsesCorrectOverload() {
      const string? input = "BoolId-True";
      var success = StronglyTypedId<BoolId, bool>.TryParse(input, null, out var result);

      Assert.True(success);
      Assert.NotNull(result);
      Assert.True(result.Value);

      // Test false value too
      var successFalse = StronglyTypedId<BoolId, bool>.TryParse("BoolId-False", null, out var resultFalse);
      Assert.True(successFalse);
      Assert.False(resultFalse!.Value);
    }
    [Fact]
    public void Given_TypeWithParseFormatProvider_When_TryParse_Then_UsesCorrectOverload() {
      const string? input = "CustomTypeWithParseFormatProviderId-123";
      var success = StronglyTypedId<CustomTypeWithParseFormatProviderId, CustomTypeWithParseFormatProvder>.TryParse(
          input, CultureInfo.InvariantCulture, out var result);

      Assert.True(success);
      Assert.NotNull(result);
      Assert.Equal(123, result.Value.Value);
      const string? germanInput = "CustomTypeWithParseFormatProviderId-123";
      var germanCulture = new CultureInfo("de-DE");
      var germanSuccess = StronglyTypedId<CustomTypeWithParseFormatProviderId, CustomTypeWithParseFormatProvder>.TryParse(
          germanInput, germanCulture, out var germanResult);

      Assert.True(germanSuccess);
      Assert.NotNull(germanResult);
      Assert.Equal(123, germanResult.Value.Value);
      const string? invalidInput = "CustomTypeWithParseFormatProviderId-abc";
      var invalidSuccess = StronglyTypedId<CustomTypeWithParseFormatProviderId, CustomTypeWithParseFormatProvder>.TryParse(
          invalidInput, CultureInfo.InvariantCulture, out _);

      Assert.False(invalidSuccess);
    }

    [Fact]
    public void Given_TypeWithParseWithoutFormatProvider_When_TryParse_Then_UsesCorrectOverload() {
      const string? input = "CustomTypeWithParseWithoutFormatProviderId-42";
      var success = StronglyTypedId<CustomTypeWithParseWithoutFormatProviderId, CustomTypeWithParseWithoutFormatProvder>.TryParse(
          input, null, out var result);

      Assert.True(success);
      Assert.NotNull(result);
      Assert.Equal(42, result.Value.Value);
      const string? invalidInput = "CustomTypeWithParseWithoutFormatProviderId-not-a-number";
      var invalidSuccess = StronglyTypedId<CustomTypeWithParseWithoutFormatProviderId, CustomTypeWithParseWithoutFormatProvder>.TryParse(
          invalidInput, null, out _);

      Assert.False(invalidSuccess);
    }

    [Fact]
    public void Given_ComparableValues_When_Compared_Then_OrderedCorrectly() {
      var id1 = new IntegerId(1);
      var id2 = new IntegerId(2);
      var alsoId1 = new IntegerId(1);

      // Test value equality from record semantics
      Assert.Equal(id1, alsoId1);
      Assert.NotEqual(id1, id2);

      // Create a list and verify sorting works
      var list = new List<IntegerId> { id2, id1 };
      list.Sort((a, b) => a.Value.CompareTo(b.Value));

      // Then
      Assert.Equal(id1, list[0]);
      Assert.Equal(id2, list[1]);
    }

    [Fact]
    public void Given_StronglyTypedIdWithCustomFormat_When_ToString_Then_UsesProvidedFormat() {
      var decimalId = new DecimalIdWithCustomFormat(123.456m);

      var formatted = decimalId.ToString();

      Assert.StartsWith("DecimalIdWithCustomFormat-", formatted, StringComparison.InvariantCulture);
      Assert.EndsWith("-123.46", formatted, StringComparison.InvariantCulture);

      // Verify round-trip
      var success = StronglyTypedId<DecimalIdWithCustomFormat, decimal>.TryParse(
          formatted, CultureInfo.InvariantCulture, out var parsedId);
      Assert.True(success);
      Assert.NotNull(parsedId);
      Assert.Equal(123.456m, parsedId.Value, 2);
    }

    // Helper record types for testing
    internal sealed record StringId(string Value): StronglyTypedId<StringId, string>(Value) {
      public override string ToString() => ToSerializedString(this);
    }
    internal sealed record IntegerId(int Value): StronglyTypedId<IntegerId, int>(Value) {
      public override string ToString() => ToSerializedString(this);
    }
    internal sealed record DateTimeId(DateTime Value): StronglyTypedId<DateTimeId, DateTime>(Value) {
      public override string ToString() => ToSerializedString(this);
    }
    internal sealed class CustomTypeWithParseFormatProvder {
      public int Value { get; init; }
      public static CustomTypeWithParseFormatProvder Parse(string input, IFormatProvider? provider) =>
        // Simulate parsing logic
        new() { Value = int.Parse(input, provider) };
    }
    internal sealed class CustomTypeWithParseWithoutFormatProvder {
      public int Value { get; init; }
      public static CustomTypeWithParseWithoutFormatProvder Parse(string input) =>
        // Simulate parsing logic
        new() { Value = int.Parse(input, CultureInfo.InvariantCulture) };
    }

    // Custom type without Parse/TryParse for testing exception
    internal sealed class CustomType { public string Value { get; } = "test"; }

    // Helper record for URI-based ID to test Parse without TryParse
    internal sealed record UriId(Uri Value): StronglyTypedId<UriId, Uri>(Value) {
      public override string ToString() => ToSerializedString(this);
    }

    // Helper record for bool-based ID to test TryParse without IFormatProvider
    internal sealed record BoolId(bool Value): StronglyTypedId<BoolId, bool>(Value) {
      public override string ToString() => ToSerializedString(this);
    }
    internal sealed record CustomTypeId(CustomType Value): StronglyTypedId<CustomTypeId, CustomType>(Value) {
      public override string ToString() => ToSerializedString(this);
    }
    internal sealed record CustomTypeWithParseWithoutFormatProviderId(CustomTypeWithParseWithoutFormatProvder Value)
      : StronglyTypedId<CustomTypeWithParseWithoutFormatProviderId, CustomTypeWithParseWithoutFormatProvder>(Value) {
      public override string ToString() => ToSerializedString(this);
    }
    internal sealed record CustomTypeWithParseFormatProviderId(CustomTypeWithParseFormatProvder Value)
      : StronglyTypedId<CustomTypeWithParseFormatProviderId, CustomTypeWithParseFormatProvder>(Value) {
      public override string ToString() => ToSerializedString(this);
    }

    internal sealed record DecimalIdWithCustomFormat(decimal Value)
      : StronglyTypedId<DecimalIdWithCustomFormat, decimal>(Value) {
      public override string ToString() =>
        // Custom format with 2 decimal places
        $"{GetType().Name}-{Value.ToString("0.00", CultureInfo.InvariantCulture)}";
    }
  }
}
