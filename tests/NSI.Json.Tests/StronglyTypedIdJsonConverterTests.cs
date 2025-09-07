using System.Globalization;
using System.Text.Json;
using NSI.Domains;
using NSI.Domains.StrongIdentifier;
using NSI.Json.StrongIdentifier;

namespace NSI.Json.Tests;
public class StronglyTypedIdJsonConverterTests {
  // Cached JsonSerializerOptions for all tests
  private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new() {
    Converters = { new StronglyTypedIdJsonConverterFactory() }
  };

  [Fact]
  public void Given_ObjectWithNonNullableId_When_SerializedAndDeserialized_Then_OriginalValuesArePreserved() {
    // Given
    var user = new NonNullablePropertyTestClass {
      Id = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture)),
      Email = "test@test.com"
    };

    // When
    var json = JsonSerializer.Serialize(user, CachedJsonSerializerOptions);
    var deserialized = JsonSerializer.Deserialize<NonNullablePropertyTestClass>(json, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("UserId-00000000-0000-0000-0000-000000000000", json, StringComparison.InvariantCulture);
    Assert.Contains("test@test.com", json, StringComparison.InvariantCulture);
    Assert.NotNull(deserialized);
    Assert.Equal(user.Id.Value, deserialized.Id.Value);
    Assert.Equal(user.Email, deserialized.Email);
  }

  [Fact]
  public void Given_ObjectWithNullId_When_Serialized_Then_JsonContainsNullIdProperty() {
    // Given
    var obj = new NullablePropertyTestClass { Id = null, Email = "test@test.com" };

    // When
    var json = JsonSerializer.Serialize(obj, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("\"Id\":null", json, StringComparison.InvariantCulture);
    Assert.Contains("test@test.com", json, StringComparison.InvariantCulture);
  }

  [Fact]
  public void Given_ObjectWithNonNullId_When_Serialized_Then_JsonContainsFormattedIdValue() {
    // Given
    var id = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture));
    var obj = new NullablePropertyTestClass { Id = id, Email = "test@test.com" };

    // When
    var json = JsonSerializer.Serialize(obj, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("UserId-00000000-0000-0000-0000-000000000000", json, StringComparison.InvariantCulture);
  }

  [Theory]
  [InlineData("{\"Id\":null,\"Email\":\"test@test.com\"}", null, "test@test.com")]
  [InlineData("{\"Id\":\"UserId-00000000-0000-0000-0000-000000000000\",\"Email\":\"test@test.com\"}", "00000000-0000-0000-0000-000000000000", "test@test.com")]
  public void Given_JsonWithIdProperty_When_Deserialized_Then_PropertiesAreCorrectlyPopulated(
      string json, string? expectedGuid, string expectedEmail) {
    // Given - json from parameter

    // When
    var obj = JsonSerializer.Deserialize<NullablePropertyTestClass>(json, CachedJsonSerializerOptions);

    // Then
    Assert.NotNull(obj);
    Assert.Equal(expectedEmail, obj.Email);

    if (expectedGuid == null) {
      Assert.Null(obj.Id);
    } else {
      Assert.NotNull(obj.Id);
      Assert.Equal(Guid.Parse(expectedGuid, CultureInfo.InvariantCulture), obj.Id.Value);
    }
  }
  [Fact]
  public void Given_JsonWithInvalidIdFormat_When_Deserialized_Then_ThrowsJsonException() {
    // Given
    var invalidJson = "{\"Id\":\"InvalidFormat\",\"Email\":\"test@test.com\"}";

    // When/Then
    var exception = Assert.Throws<JsonException>(() =>
        JsonSerializer.Deserialize<NonNullablePropertyTestClass>(invalidJson, CachedJsonSerializerOptions));

    Assert.Contains("Invalid serialized UserId", exception.Message, StringComparison.InvariantCulture);
  }

  [Fact]
  public void Given_ObjectWithIntegerId_When_SerializedAndDeserialized_Then_OriginalValueIsPreserved() {
    // Given
    var container = new IntegerIdContainer {
      Id = new IntegerId(42),
      Name = "TestInteger"
    };

    // When
    var json = JsonSerializer.Serialize(container, CachedJsonSerializerOptions);
    var deserialized = JsonSerializer.Deserialize<IntegerIdContainer>(json, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("IntegerId-42", json, StringComparison.InvariantCulture);
    Assert.NotNull(deserialized);
    Assert.Equal(42, deserialized.Id.Value);
    Assert.Equal("TestInteger", deserialized.Name);
  }

  [Fact]
  public void Given_ObjectWithStringId_When_SerializedAndDeserialized_Then_OriginalValueIsPreserved() {
    // Given
    var container = new StringIdContainer {
      Id = new StringId("abc123"),
      Name = "TestString"
    };

    // When
    var json = JsonSerializer.Serialize(container, CachedJsonSerializerOptions);
    var deserialized = JsonSerializer.Deserialize<StringIdContainer>(json, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("StringId-abc123", json, StringComparison.InvariantCulture);
    Assert.NotNull(deserialized);
    Assert.Equal("abc123", deserialized.Id.Value);
    Assert.Equal("TestString", deserialized.Name);
  }

  [Fact]
  public void Given_CollectionOfIds_When_SerializedAndDeserialized_Then_AllIdsArePreserved() {
    // Given
    var collection = new IdCollection {
      Ids = [
          new UserId(Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture)),
          new UserId(Guid.Parse("11111111-1111-1111-1111-111111111111", CultureInfo.InvariantCulture)),
          new UserId(Guid.Parse("22222222-2222-2222-2222-222222222222", CultureInfo.InvariantCulture))
        ],
      Name = "TestCollection"
    };

    // When
    var json = JsonSerializer.Serialize(collection, CachedJsonSerializerOptions);
    var deserialized = JsonSerializer.Deserialize<IdCollection>(json, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("UserId-00000000-0000-0000-0000-000000000000", json, StringComparison.InvariantCulture);
    Assert.Contains("UserId-11111111-1111-1111-1111-111111111111", json, StringComparison.InvariantCulture);
    Assert.Contains("UserId-22222222-2222-2222-2222-222222222222", json, StringComparison.InvariantCulture);
    Assert.NotNull(deserialized);
    Assert.Equal(3, deserialized.Ids.Count);
    Assert.Equal(collection.Ids[0].Value, deserialized.Ids[0].Value);
    Assert.Equal(collection.Ids[1].Value, deserialized.Ids[1].Value);
    Assert.Equal(collection.Ids[2].Value, deserialized.Ids[2].Value);
  }
  [Fact]
  public void Given_JsonWithWrongIdType_When_Deserialized_Then_ThrowsJsonException() {
    // Given - JSON with an IntegerId format but trying to deserialize as UserId
    var wrongTypeJson = "{\"Id\":\"IntegerId-123\",\"Email\":\"test@test.com\"}";

    // When/Then
    var exception = Assert.Throws<JsonException>(() =>
        JsonSerializer.Deserialize<NonNullablePropertyTestClass>(wrongTypeJson, CachedJsonSerializerOptions));

    Assert.Contains("Invalid serialized UserId", exception.Message, StringComparison.InvariantCulture);
  }
  [Fact]
  public void Given_StandaloneId_When_SerializedAndDeserialized_Then_ValueIsPreserved() {
    // Given
    var id = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture));

    // When
    var json = JsonSerializer.Serialize(id, CachedJsonSerializerOptions);
    var deserialized = JsonSerializer.Deserialize<UserId>(json, CachedJsonSerializerOptions);

    // Then
    Assert.Equal("\"UserId-00000000-0000-0000-0000-000000000000\"", json);
    Assert.NotNull(deserialized);
    Assert.Equal(id.Value, deserialized.Value);
  }

  [Fact]
  public void Given_DictionaryWithIdValues_When_SerializedAndDeserialized_Then_DictionaryIsPreserved() {
    // Given
    var container = new DictionaryContainer {
      UserMapping = new Dictionary<string, UserId> {
        ["user1"] = new UserId(Guid.Parse("00000000-0000-0000-0000-000000000000", CultureInfo.InvariantCulture)),
        ["user2"] = new UserId(Guid.Parse("11111111-1111-1111-1111-111111111111", CultureInfo.InvariantCulture))
      }
    };

    // When
    var json = JsonSerializer.Serialize(container, CachedJsonSerializerOptions);
    var deserialized = JsonSerializer.Deserialize<DictionaryContainer>(json, CachedJsonSerializerOptions);

    // Then
    Assert.Contains("UserId-00000000-0000-0000-0000-000000000000", json, StringComparison.InvariantCulture);
    Assert.Contains("UserId-11111111-1111-1111-1111-111111111111", json, StringComparison.InvariantCulture);
    Assert.NotNull(deserialized);
    Assert.Equal(2, deserialized.UserMapping.Count);
    Assert.Equal(container.UserMapping["user1"].Value, deserialized.UserMapping["user1"].Value);
    Assert.Equal(container.UserMapping["user2"].Value, deserialized.UserMapping["user2"].Value);
  }
  [Fact]
  public void Given_JsonWithNonStringIdProperty_When_Deserialized_Then_ThrowsJsonException() {
    // Given - JSON with a numeric ID value instead of a string
    var invalidTypeJson = "{\"Id\":123,\"Email\":\"test@test.com\"}";

    // When/Then
    Assert.Throws<JsonException>(() =>
        JsonSerializer.Deserialize<NonNullablePropertyTestClass>(invalidTypeJson, CachedJsonSerializerOptions));
  }

  // Dummy endpoint class for binding tests
  protected static class DummyEndpointClass {
    public static void EndpointWithNonNullable(UserId id) => throw new NotSupportedException();
    public static void EndpointWithNullable(UserId? id) => throw new NotSupportedException();
  }

  // Class under test for non-nullable property tests
  internal sealed class NonNullablePropertyTestClass {
    public required UserId Id { get; set; }
    public required string Email { get; set; }
  }

  // Class under test for nullable property tests
  internal sealed class NullablePropertyTestClass {
    public UserId? Id { get; set; }
    public required string Email { get; set; }
  }
  // Add these record definitions to your test class
  internal sealed record IntegerId(int Value): StronglyTypedId<IntegerId, int>(Value);

  internal sealed record StringId(string Value): StronglyTypedId<StringId, string>(Value);

  internal sealed class IntegerIdContainer {
    public required IntegerId Id { get; set; }
    public required string Name { get; set; }
  }

  internal sealed class StringIdContainer {
    public required StringId Id { get; set; }
    public required string Name { get; set; }
  }
  internal sealed class IdCollection {
    public required List<UserId> Ids { get; set; }
    public required string Name { get; set; }
  }
  internal sealed class DictionaryContainer {
    public required Dictionary<string, UserId> UserMapping { get; set; }
  }
}
