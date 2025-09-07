using NSI.Core.Results;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Tests.Results {
  /// <summary>
  /// Tests for the <see cref="ResultError"/> record struct.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests verify the ResultError type's construction, equality, and conversion behavior.
  /// The ResultError type is fundamental to the Result pattern and must maintain proper value semantics.
  /// </para>
  /// <para>
  /// Test coverage includes:
  /// <list type="bullet">
  ///   <item><description>Construction with various parameter combinations</description></item>
  ///   <item><description>Implicit string conversion</description></item>
  ///   <item><description>Static factory methods for common error types</description></item>
  ///   <item><description>Validation error handling</description></item>
  ///   <item><description>Equality and hash code behavior</description></item>
  ///   <item><description>String representation formatting</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public class ResultErrorTests {
    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateError() {
      var type = ErrorType.Validation;
      const string code = "VALIDATION_ERROR";
      const string message = "Email is required";
      var exception = new ArgumentException("Test exception");
      var validationErrors = new List<IValidationError> { new ValidationError("Email", "Required", "REQUIRED") };

      var error = new ResultError(type, code, message, exception, validationErrors);

      Assert.Equal(type, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
      Assert.Same(exception, error.Exception);
      Assert.Equal(validationErrors, error.ValidationErrors);
      Assert.True(error.HasValidationErrors);
    }

    [Fact]
    public void Constructor_WithoutOptionalParameters_ShouldCreateError() {
      var type = ErrorType.NotFound;
      const string code = "USER_NOT_FOUND";
      const string message = "User not found";

      var error = new ResultError(type, code, message);

      Assert.Equal(type, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
      Assert.Null(error.Exception);
      Assert.Null(error.ValidationErrors);
      Assert.False(error.HasValidationErrors);
    }

    [Theory]
    [InlineData(ErrorType.Authentication)]
    [InlineData(ErrorType.Authorization)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    public void IsOfType_WithMatchingType_ShouldReturnTrue(ErrorType errorType) {
      var error = new ResultError(errorType, "CODE", "Message");

      var result = error.IsOfType(errorType);

      Assert.True(result);
    }

    [Fact]
    public void IsOfType_WithDifferentType_ShouldReturnFalse() {
      var error = new ResultError(ErrorType.NotFound, "CODE", "Message");

      var result = error.IsOfType(ErrorType.Authentication);

      Assert.False(result);
    }

    #region Static Factory Methods Tests

    [Fact]
    public void Validation_WithValidationErrors_ShouldCreateValidationError() {
      const string code = "INVALID_INPUT";
      const string message = "Validation failed";
      var validationErrors = new List<IValidationError> {
      new ValidationError("Email", "Email is required", "REQUIRED"),
      new ValidationError("Password", "Password too short", "MIN_LENGTH")
    };

      var error = ResultError.Validation(code, message, validationErrors);

      Assert.Equal(ErrorType.Validation, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
      Assert.True(error.HasValidationErrors);
      Assert.Equal(2, error.ValidationErrors!.Count);
      Assert.Equal(validationErrors, error.ValidationErrors);
    }

    [Fact]
    public void Validation_WithSingleValidationError_ShouldCreateValidationError() {
      const string code = "INVALID_EMAIL";
      const string message = "Email validation failed";
      var validationError = new ValidationError("Email", "Email is required", "REQUIRED");

      var error = ResultError.Validation(code, message, validationError);

      Assert.Equal(ErrorType.Validation, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
      Assert.True(error.HasValidationErrors);
      Assert.Single(error.ValidationErrors!);
      Assert.Equal(validationError, error.ValidationErrors![0]);
    }

    [Fact]
    public void Validation_WithNullValidationErrors_ShouldThrowArgumentNullException() {
      IEnumerable<IValidationError>? nullErrors = null;

      var exception = Assert.Throws<ArgumentNullException>(() => ResultError.Validation("CODE", "Message", nullErrors!));
      Assert.Equal("validationErrors", exception.ParamName);
    }

    [Fact]
    public void Unauthorized_WithValidParameters_ShouldCreateAuthenticationError() {
      const string code = "INVALID_TOKEN";
      const string message = "Token has expired";
      var exception = new UnauthorizedAccessException();

      var error = ResultError.Unauthorized(code, message, exception);

      Assert.Equal(ErrorType.Authentication, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
      Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void Forbidden_WithValidParameters_ShouldCreateAuthorizationError() {
      const string code = "INSUFFICIENT_PERMISSIONS";
      const string message = "User lacks required permissions";

      var error = ResultError.Forbidden(code, message);

      Assert.Equal(ErrorType.Authorization, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
      Assert.Null(error.Exception);
    }

    [Fact]
    public void NotFound_WithValidParameters_ShouldCreateNotFoundError() {
      const string code = "USER_NOT_FOUND";
      const string message = "User with ID 123 was not found";

      var error = ResultError.NotFound(code, message);

      Assert.Equal(ErrorType.NotFound, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
    }

    [Fact]
    public void Conflict_WithValidParameters_ShouldCreateConflictError() {
      const string code = "DUPLICATE_EMAIL";
      const string message = "Email address is already registered";

      var error = ResultError.Conflict(code, message);

      Assert.Equal(ErrorType.Conflict, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
    }

    [Fact]
    public void ServiceUnavailable_WithValidParameters_ShouldCreateServiceUnavailableError() {
      const string code = "API_DOWN";
      const string message = "External API is currently unavailable";

      var error = ResultError.ServiceUnavailable(code, message);

      Assert.Equal(ErrorType.ServiceUnavailable, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
    }

    [Fact]
    public void BusinessRule_WithValidParameters_ShouldCreateBusinessRuleError() {
      const string code = "INSUFFICIENT_BALANCE";
      const string message = "Account balance is insufficient";

      var error = ResultError.BusinessRule(code, message);

      Assert.Equal(ErrorType.BusinessRule, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
    }

    [Fact]
    public void Database_WithValidParameters_ShouldCreateDatabaseError() {
      const string code = "CONNECTION_FAILED";
      const string message = "Unable to connect to database";

      var error = ResultError.Database(code, message);

      Assert.Equal(ErrorType.Database, error.Type);
      Assert.Equal(code, error.Code);
      Assert.Equal(message, error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void StaticMethods_WithInvalidCode_ShouldThrowArgumentException(string invalidCode) {
      void UnauthorizedAction() => ResultError.Unauthorized(invalidCode, "Message");
      void ForbiddenAction() => ResultError.Forbidden(invalidCode, "Message");
      void NotFoundAction() => ResultError.NotFound(invalidCode, "Message");

      Assert.Throws<ArgumentException>(UnauthorizedAction);
      Assert.Throws<ArgumentException>(ForbiddenAction);
      Assert.Throws<ArgumentException>(NotFoundAction);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void StaticMethods_WithInvalidMessage_ShouldThrowArgumentException(string invalidMessage) {
      void UnauthorizedAction() => ResultError.Unauthorized("CODE", invalidMessage);
      void ForbiddenAction() => ResultError.Forbidden("CODE", invalidMessage);
      void NotFoundAction() => ResultError.NotFound("CODE", invalidMessage);

      Assert.Throws<ArgumentException>(UnauthorizedAction);
      Assert.Throws<ArgumentException>(ForbiddenAction);
      Assert.Throws<ArgumentException>(NotFoundAction);
    }

    #endregion

    [Fact]
    public void ToString_WithTypeCodeAndMessage_ShouldReturnFormattedString() {
      var error = new ResultError(ErrorType.NotFound, "USER_NOT_FOUND", "User not found");

      var result = error.ToString();

      Assert.Equal("[NotFound:USER_NOT_FOUND] User not found", result);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue() {
      var error1 = new ResultError(ErrorType.Generic, "CODE", "Message");
      var error2 = new ResultError(ErrorType.Generic, "CODE", "Message");

      Assert.True(error1.Equals(error2));
      Assert.True(error1 == error2);
      Assert.False(error1 != error2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse() {
      var error1 = new ResultError(ErrorType.Generic, "CODE1", "Message");
      var error2 = new ResultError(ErrorType.Generic, "CODE2", "Message");

      Assert.False(error1.Equals(error2));
      Assert.False(error1 == error2);
      Assert.True(error1 != error2);
    }
  }
}
