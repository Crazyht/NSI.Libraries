using NSI.Core.Validation;

namespace NSI.Core.Tests.Validation;
/// <summary>
/// Tests for the <see cref="ValidationError"/> class.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the proper construction and behavior of validation errors
/// including parameter validation and property assignment.
/// </para>
/// </remarks>
public sealed class ValidationErrorTests {
  [Fact]
  public void Constructor_WithValidParameters_ShouldSetAllProperties() {
    var error = new ValidationError(
      errorCode: "INVALID_EMAIL",
      errorMessage: "Email is invalid",
      propertyName: "Email",
      expectedValue: "valid@email.com"
    );

    Assert.Equal("INVALID_EMAIL", error.ErrorCode);
    Assert.Equal("Email is invalid", error.ErrorMessage);
    Assert.Equal("Email", error.PropertyName);
    Assert.Equal("valid@email.com", error.ExpectedValue);
  }

  [Fact]
  public void Constructor_WithNullPropertyNameAndExpectedValue_ShouldAllowNullValues() {
    var error = new ValidationError(
      errorCode: "CROSS_FIELD_ERROR",
      errorMessage: "Cross field validation failed",
      propertyName: null,
      expectedValue: null
    );

    Assert.Null(error.PropertyName);
    Assert.Null(error.ExpectedValue);
  }

  [Fact]
  public void Constructor_WithInvalidErrorCode_ShouldThrowArgumentNullException() {
    var exception = Assert.Throws<ArgumentNullException>(
      () => new ValidationError(null!, "Message")
    );

    Assert.Contains("errorCode", exception.Message, StringComparison.Ordinal);
  }
  [Theory]
  [InlineData("")]
  [InlineData("    ")]
  public void Constructor_WithInvalidErrorCode_ShouldThrowArgumentException(string? errorCode) {
    var exception = Assert.Throws<ArgumentException>(
      () => new ValidationError(errorCode!, "Message")
    );

    Assert.Contains("errorCode", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public void Constructor_WithInvalidErrorMessage_ShouldThrowArgumentNullException() {
    var exception = Assert.Throws<ArgumentNullException>(
      () => new ValidationError("ERROR_CODE", null!)
    );

    Assert.Contains("errorMessage", exception.Message, StringComparison.Ordinal);
  }

  [Theory]
  [InlineData("")]
  [InlineData("    ")]
  public void Constructor_WithInvalidErrorMessage_ShouldThrowArgumentException(string? errorMessage) {
    var exception = Assert.Throws<ArgumentException>(
      () => new ValidationError("ERROR_CODE", errorMessage!)
    );

    Assert.Contains("errorMessage", exception.Message, StringComparison.Ordinal);
  }
}
