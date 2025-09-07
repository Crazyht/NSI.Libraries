using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Tests.Validation {
  /// <summary>
  /// Tests for the <see cref="ValidationResult"/> class.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests verify validation result creation, success/failure states,
  /// and helper method functionality.
  /// </para>
  /// </remarks>
  public sealed class ValidationResultTests {
    [Fact]
    public void Success_ShouldReturnValidResultWithNoErrors() {
      var result = ValidationResult.Success;

      Assert.True(result.IsValid);
      Assert.Empty(result.Errors);
    }

    [Fact]
    public void Constructor_WithEmptyErrors_ShouldCreateValidResult() {
      var result = new ValidationResult([]);

      Assert.True(result.IsValid);
      Assert.Empty(result.Errors);
    }

    [Fact]
    public void Constructor_WithErrors_ShouldCreateInvalidResult() {
      var errors = new IValidationError[] {
        new ValidationError("ERROR1", "First error"),
        new ValidationError("ERROR2", "Second error")
      };

      var result = new ValidationResult(errors);

      Assert.False(result.IsValid);
      Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Constructor_WithNullErrors_ShouldThrowArgumentNullException() {
      var exception = Assert.Throws<ArgumentNullException>(
        () => new ValidationResult(null!)
      );

      Assert.Equal("errors", exception.ParamName);
    }

    [Fact]
    public void Failed_WithErrorArray_ShouldCreateInvalidResult() {
      var error1 = new ValidationError("ERROR1", "Message 1");
      var error2 = new ValidationError("ERROR2", "Message 2");

      var result = ValidationResult.Failed(error1, error2);

      Assert.False(result.IsValid);
      Assert.Equal(2, result.Errors.Count);
      Assert.Contains(result.Errors, e => e.ErrorCode == "ERROR1");
      Assert.Contains(result.Errors, e => e.ErrorCode == "ERROR2");
    }

    [Fact]
    public void Failed_WithErrorDetails_ShouldCreateInvalidResultWithSingleError() {
      var result = ValidationResult.Failed(
        "INVALID_VALUE",
        "Value is invalid",
        "TestProperty",
        42
      );

      Assert.False(result.IsValid);
      Assert.Single(result.Errors);

      var error = result.Errors[0];
      Assert.Equal("INVALID_VALUE", error.ErrorCode);
      Assert.Equal("Value is invalid", error.ErrorMessage);
      Assert.Equal("TestProperty", error.PropertyName);
      Assert.Equal(42, error.ExpectedValue);
    }
  }
}
