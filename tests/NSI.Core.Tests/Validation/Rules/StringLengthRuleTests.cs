using NSI.Core.Validation;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Tests.Validation.Rules {
  /// <summary>
  /// Tests for the <see cref="StringLengthRule{T}"/> validation rule.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests verify string length validation including minimum/maximum
  /// constraints and boundary value handling.
  /// </para>
  /// </remarks>
  public sealed class StringLengthRuleTests {
    private sealed class TestModel {
      public string? Value { get; set; }
    }

    [Theory]
    [InlineData("abc", 1, 5)]
    [InlineData("12345", 5, 5)]
    [InlineData("", 0, 10)]
    [InlineData("exactly10!", 10, 10)]
    public void Validate_WithValidLength_ShouldReturnNoErrors(string value, int min, int max) {
      var rule = new StringLengthRule<TestModel>(m => m.Value, min, max);
      var model = new TestModel { Value = value };
      var context = ValidationContext.Empty();

      var errors = rule.Validate(model, context);

      Assert.Empty(errors);
    }

    [Theory]
    [InlineData("ab", 3, "TOO_SHORT", 3)]
    [InlineData("", 1, "TOO_SHORT", 1)]
    [InlineData("short", 10, "TOO_SHORT", 10)]
    public void Validate_WithTooShortValue_ShouldReturnTooShortError(
      string value,
      int minLength,
      string expectedCode,
      int expectedLength) {
      var rule = new StringLengthRule<TestModel>(m => m.Value, minLength, 100);
      var model = new TestModel { Value = value };
      var context = ValidationContext.Empty();

      var errors = rule.Validate(model, context).ToList();

      Assert.Single(errors);
      Assert.Equal(expectedCode, errors[0].ErrorCode);
      Assert.Contains($"at least {minLength} characters", errors[0].ErrorMessage, StringComparison.Ordinal);
      Assert.Equal("Value", errors[0].PropertyName);
      Assert.Equal(expectedLength, errors[0].ExpectedValue);
    }

    [Theory]
    [InlineData("too long", 5, "TOO_LONG", 5)]
    [InlineData("123456", 5, "TOO_LONG", 5)]
    [InlineData("x", 0, "TOO_LONG", 0)]
    public void Validate_WithTooLongValue_ShouldReturnTooLongError(
      string value,
      int maxLength,
      string expectedCode,
      int expectedLength) {
      var rule = new StringLengthRule<TestModel>(m => m.Value, 0, maxLength);
      var model = new TestModel { Value = value };
      var context = ValidationContext.Empty();

      var errors = rule.Validate(model, context).ToList();

      Assert.Single(errors);
      Assert.Equal(expectedCode, errors[0].ErrorCode);
      Assert.Contains($"not exceed {maxLength} characters", errors[0].ErrorMessage, StringComparison.Ordinal);
      Assert.Equal("Value", errors[0].PropertyName);
      Assert.Equal(expectedLength, errors[0].ExpectedValue);
    }

    [Fact]
    public void Validate_WithNullValue_ShouldReturnNoErrors() {
      var rule = new StringLengthRule<TestModel>(m => m.Value, 5, 10);
      var model = new TestModel { Value = null };
      var context = ValidationContext.Empty();

      var errors = rule.Validate(model, context);

      Assert.Empty(errors);
    }

    [Fact]
    public void Constructor_WithNegativeMinLength_ShouldThrowArgumentOutOfRangeException() {
      var exception = Assert.Throws<ArgumentOutOfRangeException>(
        () => new StringLengthRule<TestModel>(m => m.Value, -1, 10)
      );

      Assert.Equal("minLength", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithMaxLessThanMin_ShouldThrowArgumentOutOfRangeException() {
      var exception = Assert.Throws<ArgumentOutOfRangeException>(
        () => new StringLengthRule<TestModel>(m => m.Value, 10, 5)
      );

      Assert.Equal("maxLength", exception.ParamName);
    }
  }
}
