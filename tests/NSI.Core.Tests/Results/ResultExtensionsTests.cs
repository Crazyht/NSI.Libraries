using NSI.Core.Results;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Tests.Results;
/// <summary>
/// Tests for the <see cref="ResultExtensions"/> class.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the extension methods for Result types.
/// Coverage includes filtering, error type checking, and conversion operations.
/// </para>
/// </remarks>
public class ResultExtensionsTests {
  #region Where Tests

  [Fact]
  public void Where_WithSuccessResultAndPredicateTrue_ShouldReturnSuccess() {
    var result = Result.Success(10);

    var filtered = result.Where(x => x > 5, () => ResultError.BusinessRule("TOO_SMALL", "Value too small"));

    Assert.True(filtered.IsSuccess);
    Assert.Equal(10, filtered.Value);
  }

  [Fact]
  public void Where_WithSuccessResultAndPredicateFalse_ShouldReturnFailure() {
    var result = Result.Success(3);
    var expectedError = ResultError.BusinessRule("TOO_SMALL", "Value too small");

    var filtered = result.Where(x => x > 5, () => expectedError);

    Assert.True(filtered.IsFailure);
    Assert.Equal(expectedError, filtered.Error);
  }

  [Fact]
  public void Where_WithFailureResult_ShouldPropagateError() {
    var originalError = ResultError.NotFound("ORIGINAL", "Original error");
    var result = Result.Failure<int>(originalError);

    var filtered = result.Where(x => x > 5, () => ResultError.BusinessRule("NEW", "New error"));

    Assert.True(filtered.IsFailure);
    Assert.Equal(originalError, filtered.Error);
  }

  [Fact]
  public void Where_WithNullPredicate_ShouldThrowArgumentNullException() {
    var result = Result.Success(10);

    var ex = Assert.Throws<ArgumentNullException>(() => result.Where(null!, () => ResultError.BusinessRule("ERROR", "Error")));
    Assert.Equal("predicate", ex.ParamName);
  }

  [Fact]
  public void Where_WithNullErrorFactory_ShouldThrowArgumentNullException() {
    var result = Result.Success(10);

    var ex = Assert.Throws<ArgumentNullException>(() => result.Where(x => true, null!));
    Assert.Equal("errorFactory", ex.ParamName);
  }

  #endregion

  #region IsFailureOfType Tests

  [Fact]
  public void IsFailureOfType_WithMatchingErrorType_ShouldReturnTrue() {
    var result = Result.Failure<int>(ResultError.NotFound("TEST", "Not found"));

    var isNotFound = result.IsFailureOfType(ErrorType.NotFound);

    Assert.True(isNotFound);
  }

  [Fact]
  public void IsFailureOfType_WithDifferentErrorType_ShouldReturnFalse() {
    var result = Result.Failure<int>(ResultError.NotFound("TEST", "Not found"));

    var isValidation = result.IsFailureOfType(ErrorType.Validation);

    Assert.False(isValidation);
  }

  [Fact]
  public void IsFailureOfType_WithSuccessResult_ShouldReturnFalse() {
    var result = Result.Success(42);

    var isFailure = result.IsFailureOfType(ErrorType.NotFound);

    Assert.False(isFailure);
  }

  #endregion

  #region TapErrorOfType Tests

  [Fact]
  public void TapErrorOfType_WithMatchingErrorType_ShouldExecuteAction() {
    var error = ResultError.NotFound("TEST", "Not found");
    var result = Result.Failure<int>(error);
    var actionExecuted = false;

    var tappedResult = result.TapErrorOfType(ErrorType.NotFound, err => actionExecuted = true);

    Assert.True(actionExecuted);
    Assert.Equal(result, tappedResult);
  }

  [Fact]
  public void TapErrorOfType_WithDifferentErrorType_ShouldNotExecuteAction() {
    var error = ResultError.NotFound("TEST", "Not found");
    var result = Result.Failure<int>(error);
    var actionExecuted = false;

    var tappedResult = result.TapErrorOfType(ErrorType.Validation, err => actionExecuted = true);

    Assert.False(actionExecuted);
    Assert.Equal(result, tappedResult);
  }

  [Fact]
  public void TapErrorOfType_WithSuccessResult_ShouldNotExecuteAction() {
    var result = Result.Success(42);
    var actionExecuted = false;

    var tappedResult = result.TapErrorOfType(ErrorType.NotFound, err => actionExecuted = true);

    Assert.False(actionExecuted);
    Assert.Equal(result, tappedResult);
  }

  [Fact]
  public void TapErrorOfType_WithNullAction_ShouldThrowArgumentNullException() {
    var result = Result.Failure<int>(ResultError.NotFound("TEST", "Not found"));

    var exception = Assert.Throws<ArgumentNullException>(() => result.TapErrorOfType(ErrorType.NotFound, null!));
    Assert.Equal("action", exception.ParamName);
  }

  #endregion

  #region GetValidationErrors Tests

  [Fact]
  public void GetValidationErrors_WithValidationError_ShouldReturnValidationErrors() {
    var validationErrors = new List<IValidationError> {
      new ValidationError("Email", "Email is required", "REQUIRED"),
      new ValidationError("Password", "Password too short", "MIN_LENGTH")
    };
    var error = ResultError.Validation("INVALID_INPUT", "Validation failed", validationErrors);
    var result = Result.Failure<string>(error);

    var extractedErrors = result.GetValidationErrors();

    Assert.Equal(2, extractedErrors.Count);
    Assert.Equal(validationErrors, extractedErrors);
  }

  [Fact]
  public void GetValidationErrors_WithNonValidationError_ShouldReturnEmptyList() {
    var result = Result.Failure<string>(ResultError.NotFound("TEST", "Not found"));

    var validationErrors = result.GetValidationErrors();

    Assert.Empty(validationErrors);
  }

  [Fact]
  public void GetValidationErrors_WithSuccessResult_ShouldReturnEmptyList() {
    var result = Result.Success("test");

    var validationErrors = result.GetValidationErrors();

    Assert.Empty(validationErrors);
  }

  #endregion

  #region ToResult Tests

  [Fact]
  public void ToResult_WithNonNullReferenceType_ShouldReturnSuccess() {
    const string value = "test string";

    var result = value.ToResult(() => ResultError.NotFound("NULL_VALUE", "Value is null"));

    Assert.True(result.IsSuccess);
    Assert.Equal(value, result.Value);
  }

  [Fact]
  public void ToResult_WithNullReferenceType_ShouldReturnFailure() {
    string? nullValue = null;
    var expectedError = ResultError.NotFound("NULL_VALUE", "Value is null");

    var result = nullValue.ToResult(() => expectedError);

    Assert.True(result.IsFailure);
    Assert.Equal(expectedError, result.Error);
  }

  [Fact]
  public void ToResult_WithNullableValueTypeHasValue_ShouldReturnSuccess() {
    int? value = 42;

    var result = value.ToResult(() => ResultError.NotFound("NULL_VALUE", "Value is null"));

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void ToResult_WithNullableValueTypeNull_ShouldReturnFailure() {
    int? nullValue = null;
    var expectedError = ResultError.NotFound("NULL_VALUE", "Value is null");

    var result = nullValue.ToResult(() => expectedError);

    Assert.True(result.IsFailure);
    Assert.Equal(expectedError, result.Error);
  }

  [Fact]
  public void ToResult_WithNullErrorFactory_ShouldThrowArgumentNullException() {
    const string value = "test";

    var exception = Assert.Throws<ArgumentNullException>(() => value.ToResult(null!));
    Assert.Equal("errorFactory", exception.ParamName);
  }

  #endregion
}
