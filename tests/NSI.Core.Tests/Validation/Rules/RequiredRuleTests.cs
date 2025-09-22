using NSI.Core.Validation;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Tests.Validation.Rules;
/// <summary>
/// Tests for the <see cref="RequiredRule{T}"/> validation rule.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that required field validation properly handles
/// null values, empty strings, and whitespace-only strings.
/// </para>
/// </remarks>
public sealed class RequiredRuleTests {
  private sealed class TestModel {
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
    public object? ObjectValue { get; set; }
  }

  [Fact]
  public void Validate_WithNonNullStringValue_ShouldReturnNoErrors() {
    var rule = new RequiredRule<TestModel>(m => m.StringValue);
    var model = new TestModel { StringValue = "Valid Value" };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("   ")]
  [InlineData("\t")]
  [InlineData("\n")]
  public void Validate_WithInvalidStringValue_ShouldReturnError(string? value) {
    var rule = new RequiredRule<TestModel>(m => m.StringValue);
    var model = new TestModel { StringValue = value };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context).ToList();

    Assert.Single(errors);
    Assert.Equal("REQUIRED", errors[0].ErrorCode);
    Assert.Equal("StringValue is required.", errors[0].ErrorMessage);
    Assert.Equal("StringValue", errors[0].PropertyName);
    Assert.Equal("non-empty value", errors[0].ExpectedValue);
  }

  [Fact]
  public void Validate_WithNonNullIntValue_ShouldReturnNoErrors() {
    var rule = new RequiredRule<TestModel>(m => m.IntValue);
    var model = new TestModel { IntValue = 42 };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Fact]
  public void Validate_WithNullIntValue_ShouldReturnError() {
    var rule = new RequiredRule<TestModel>(m => m.IntValue);
    var model = new TestModel { IntValue = null };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context).ToList();

    Assert.Single(errors);
    Assert.Equal("REQUIRED", errors[0].ErrorCode);
    Assert.Equal("IntValue is required.", errors[0].ErrorMessage);
  }

  [Fact]
  public void Validate_WithZeroIntValue_ShouldReturnNoErrors() {
    var rule = new RequiredRule<TestModel>(m => m.IntValue);
    var model = new TestModel { IntValue = 0 };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Fact]
  public void Validate_WithNullObjectValue_ShouldReturnError() {
    var rule = new RequiredRule<TestModel>(m => m.ObjectValue);
    var model = new TestModel { ObjectValue = null };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context).ToList();

    Assert.Single(errors);
    Assert.Equal("REQUIRED", errors[0].ErrorCode);
    Assert.Equal("ObjectValue is required.", errors[0].ErrorMessage);
    Assert.Equal("ObjectValue", errors[0].PropertyName);
    Assert.Equal("non-empty value", errors[0].ExpectedValue);
  }

  [Fact]
  public void Validate_WithNonNullObjectValue_ShouldReturnNoErrors() {
    var rule = new RequiredRule<TestModel>(m => m.ObjectValue);
    var model = new TestModel { ObjectValue = new object() };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }
}
