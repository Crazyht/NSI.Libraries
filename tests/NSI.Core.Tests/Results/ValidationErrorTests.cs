using NSI.Core.Validation;

namespace NSI.Core.Tests.Results;
/// <summary>
/// Tests for the <see cref="ValidationError"/> record struct.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the ValidationError implementation of IValidationError.
/// </para>
/// </remarks>
public class ValidationErrorTests {
  [Fact]
  public void Constructor_WithAllParameters_ShouldCreateValidationError() {
    const string propertyName = "Email";
    const string message = "Email is required";
    const string code = "REQUIRED";

    var error = new ValidationError(code, message, propertyName);

    Assert.Equal(propertyName, error.PropertyName);
    Assert.Equal(message, error.ErrorMessage);
    Assert.Equal(code, error.ErrorCode);
  }

  [Fact]
  public void Constructor_WithNullFieldName_ShouldCreateValidationError() {
    const string message = "General validation error";
    const string code = "GENERAL";

    var error = new ValidationError(code, message, null!);

    Assert.Null(error.PropertyName);
    Assert.Equal(message, error.ErrorMessage);
    Assert.Equal(code, error.ErrorCode);
  }

  [Fact]
  public void Equals_WithSameValues_ShouldReturnTrue() {
    var error1 = new ValidationError("CODE", "Message", "Property");
    var error2 = new ValidationError("CODE", "Message", "Property");

    Assert.True(error1.Equals(error2));
    Assert.True(error1 == error2);
  }

  [Fact]
  public void Equals_WithDifferentValues_ShouldReturnFalse() {
    var error1 = new ValidationError("CODE", "Message", "Property1");
    var error2 = new ValidationError("CODE", "Message", "Property2");

    Assert.False(error1.Equals(error2));
    Assert.False(error1 == error2);
  }
}
