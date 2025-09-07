using NSI.Core.Validation;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Tests.Validation.Rules;
/// <summary>
/// Tests for the <see cref="EmailRule{T}"/> validation rule.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify email format validation including proper handling
/// of valid addresses, invalid formats, and edge cases.
/// </para>
/// </remarks>
public sealed class EmailRuleTests {
  private sealed class TestModel {
    public string? Email { get; set; }
  }

  [Theory]
  [InlineData("user@example.com")]
  [InlineData("test.user@example.com")]
  [InlineData("test+tag@example.co.uk")]
  [InlineData("123@test.org")]
  [InlineData("a@b.c")]
  public void Validate_WithValidEmail_ShouldReturnNoErrors(string email) {
    var rule = new EmailRule<TestModel>(m => m.Email);
    var model = new TestModel { Email = email };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }

  [Theory]
  [InlineData("invalid")]
  [InlineData("@example.com")]
  [InlineData("user@")]
  [InlineData("user@@example.com")]
  [InlineData("user@example")]
  [InlineData("user example@test.com")]
  [InlineData("user@example .com")]
  public void Validate_WithInvalidEmail_ShouldReturnError(string email) {
    var rule = new EmailRule<TestModel>(m => m.Email);
    var model = new TestModel { Email = email };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context).ToList();

    Assert.Single(errors);
    Assert.Equal("INVALID_EMAIL", errors[0].ErrorCode);
    Assert.Equal("Email must be a valid email address.", errors[0].ErrorMessage);
    Assert.Equal("Email", errors[0].PropertyName);
    Assert.Equal("valid@email.com", errors[0].ExpectedValue);
  }

  [Fact]
  public void Validate_WithNullEmail_ShouldReturnNoErrors() {
    var rule = new EmailRule<TestModel>(m => m.Email);
    var model = new TestModel { Email = null };
    var context = ValidationContext.Empty();

    var errors = rule.Validate(model, context);

    Assert.Empty(errors);
  }
}
