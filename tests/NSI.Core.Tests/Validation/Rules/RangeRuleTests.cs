using NSI.Core.Validation;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Tests.Validation.Rules;
/// <summary>
/// Tests for the <see cref="RangeRule{T,TValue}"/> validation rule.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify numeric range validation for various data types
/// including proper boundary value handling.
/// </para>
/// </remarks>
public sealed class RangeRuleTests {
  private sealed class TestModel {
    public int IntValue { get; set; }
    public decimal DecimalValue { get; set; }
    public DateTime DateValue { get; set; }
  }

  [Theory]
  [InlineData(5, 1, 10)]
  [InlineData(1, 1, 10)]
  [InlineData(10, 1, 10)]
  [InlineData(0, -10, 10)]
  [InlineData(-5, -10, -1)]
  public void Validate_WithIntInRange_ShouldReturnNoErrors(int value, int min, int max) {
    var rule = new RangeRule<TestModel, int>(m => m.IntValue, min, max);
    var model = new TestModel { IntValue = value };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Theory]
  [InlineData(0, 1, 10, "OUT_OF_RANGE_MIN", 1)]
  [InlineData(-1, 0, 10, "OUT_OF_RANGE_MIN", 0)]
  [InlineData(11, 1, 10, "OUT_OF_RANGE_MAX", 10)]
  [InlineData(101, 0, 100, "OUT_OF_RANGE_MAX", 100)]
  public void Validate_WithIntOutOfRange_ShouldReturnError(
    int value,
    int min,
    int max,
    string expectedCode,
    int expectedValue) {
    var rule = new RangeRule<TestModel, int>(m => m.IntValue, min, max);
    var model = new TestModel { IntValue = value };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context).ToList();

    Assert.Single(errors);
    Assert.Equal(expectedCode, errors[0].ErrorCode);
    Assert.Equal("IntValue", errors[0].PropertyName);
    Assert.Equal(expectedValue, errors[0].ExpectedValue);
  }

  [Theory]
  [InlineData(5.5, 1.0, 10.0)]
  [InlineData(1.0, 1.0, 10.0)]
  [InlineData(10.0, 1.0, 10.0)]
  [InlineData(0.0001, 0.0, 1.0)]
  public void Validate_WithDecimalInRange_ShouldReturnNoErrors(
    double value,
    double min,
    double max) {
    var rule = new RangeRule<TestModel, decimal>(
      m => m.DecimalValue,
      (decimal)min,
      (decimal)max
    );
    var model = new TestModel { DecimalValue = (decimal)value };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Fact]
  public void Validate_WithDateInRange_ShouldReturnNoErrors() {
    var min = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var max = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
    var rule = new RangeRule<TestModel, DateTime>(m => m.DateValue, min, max);
    var model = new TestModel { DateValue = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc) };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Fact]
  public void Constructor_WithMinGreaterThanMax_ShouldThrowArgumentException() {
    var exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => new RangeRule<TestModel, int>(m => m.IntValue, 10, 5)
    );

    Assert.Contains("Minimum must be less than or equal to maximum.", exception.Message, StringComparison.Ordinal);
  }
}
