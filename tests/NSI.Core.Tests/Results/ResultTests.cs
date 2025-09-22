using NSI.Core.Results;

namespace NSI.Core.Tests.Results;
/// <summary>
/// Tests for the <see cref="Result{T}"/> struct.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the core functionality of the Result pattern implementation.
/// Coverage includes creation, property access, transformation operations, and equality
/// behavior.
/// </para>
/// <para>
/// Test categories:
/// <list type="bullet">
///   <item><description>Success result creation and property access</description></item>
///   <item><description>Failure result creation and property access</description></item>
///   <item><description>Transformation operations (Map, Bind, Match)</description></item>
///   <item><description>Side effect operations (Tap, TapError)</description></item>
///   <item><description>Equality and comparison behavior</description></item>
///   <item><description>Implicit conversion operators</description></item>
/// </list>
/// </para>
/// </remarks>
public class ResultTests {
  #region Success Result Tests

  [Fact]
  public void Success_WithValue_ShouldCreateSuccessResult() {
    const int value = 42;

    var result = Result.Success(value);

    Assert.True(result.IsSuccess);
    Assert.False(result.IsFailure);
    Assert.Equal(value, result.Value);
  }

  [Fact]
  public void Success_WithNullValue_ShouldThrowArgumentNullException() {
    string? nullValue = null;

    var exception = Assert.Throws<ArgumentNullException>(() =>
      Result.Success(nullValue!));
    Assert.Equal("value", exception.ParamName);
    Assert.StartsWith(
      "Success value cannot be null",
      exception.Message,
      StringComparison.Ordinal);
  }

  [Fact]
  public void Value_OnSuccessResult_ShouldReturnValue() {
    const string expected = "test value";
    var result = Result.Success(expected);

    var actual = result.Value;

    Assert.Equal(expected, actual);
  }

  [Fact]
  public void Error_OnSuccessResult_ShouldThrowInvalidOperationException() {
    var result = Result.Success(42);

    var exception = Assert.Throws<InvalidOperationException>(() =>
      _ = result.Error);
    Assert.Equal("Cannot access Error of a success result", exception.Message);
  }

  #endregion

  #region Failure Result Tests

  [Fact]
  public void Failure_WithError_ShouldCreateFailureResult() {
    var error = ResultError.NotFound("TEST", "Test error");

    var result = Result.Failure<int>(error);

    Assert.False(result.IsSuccess);
    Assert.True(result.IsFailure);
    Assert.Equal(error, result.Error);
  }

  [Fact]
  public void Failure_WithMessage_ShouldCreateFailureResult() {
    const string message = "Test error";

    var result = Result.Failure<int>(message);

    Assert.False(result.IsSuccess);
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Generic, result.Error.Type);
    Assert.Equal("GENERIC", result.Error.Code);
    Assert.Equal(message, result.Error.Message);
  }

  [Fact]
  public void Failure_WithNullMessage_ShouldThrowArgumentNullException() =>
    Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));

  [Theory]
  [InlineData(" ")]
  [InlineData("\t")]
  [InlineData("")]
  public void Failure_WithEmptyOrSpaceMessage_ShouldThrowArgumentException(string message) =>
    Assert.Throws<ArgumentException>(() => Result.Failure<int>(message));

  [Fact]
  public void Value_OnFailureResult_ShouldThrowInvalidOperationException() {
    var result = Result.Failure<int>("Error");

    var exception = Assert.Throws<InvalidOperationException>(() =>
      _ = result.Value);
    Assert.Equal("Cannot access Value of a failure result", exception.Message);
  }

  [Fact]
  public void Error_OnFailureResult_ShouldReturnError() {
    var expectedError = ResultError.NotFound("TEST", "Test error");
    var result = Result.Failure<int>(expectedError);

    var actualError = result.Error;

    Assert.Equal(expectedError, actualError);
  }

  #endregion

  #region Map Operation Tests

  [Fact]
  public void Map_WithSuccessResult_ShouldTransformValue() {
    var result = Result.Success(5);

    var mapped = result.Map(x => x * 2);

    Assert.True(mapped.IsSuccess);
    Assert.Equal(10, mapped.Value);
  }

  [Fact]
  public void Map_WithFailureResult_ShouldPropagateError() {
    var error = ResultError.NotFound("TEST", "Test error");
    var result = Result.Failure<int>(error);

    var mapped = result.Map(x => x * 2);

    Assert.True(mapped.IsFailure);
    Assert.Equal(error, mapped.Error);
  }

  [Fact]
  public void Map_WithNullMapper_ShouldThrowArgumentNullException() {
    var result = Result.Success(5);

    var exception = Assert.Throws<ArgumentNullException>(() =>
      result.Map<int>(null!));
    Assert.Equal("mapper", exception.ParamName);
  }

  #endregion

  #region Bind Operation Tests

  [Fact]
  public void Bind_WithSuccessResult_ShouldExecuteBinder() {
    var result = Result.Success(5);

    var bound = result.Bind(x => Result.Success(x * 2));

    Assert.True(bound.IsSuccess);
    Assert.Equal(10, bound.Value);
  }

  [Fact]
  public void Bind_WithSuccessResultAndBinderReturningFailure_ShouldReturnFailure() {
    var result = Result.Success(5);
    var expectedError = ResultError.BusinessRule("TEST", "Binder error");

    var bound = result.Bind(x => Result.Failure<int>(expectedError));

    Assert.True(bound.IsFailure);
    Assert.Equal(expectedError, bound.Error);
  }

  [Fact]
  public void Bind_WithFailureResult_ShouldPropagateError() {
    var error = ResultError.NotFound("TEST", "Original error");
    var result = Result.Failure<int>(error);

    var bound = result.Bind(x => Result.Success(x * 2));

    Assert.True(bound.IsFailure);
    Assert.Equal(error, bound.Error);
  }

  [Fact]
  public void Bind_WithNullBinder_ShouldThrowArgumentNullException() {
    var result = Result.Success(5);

    var exception = Assert.Throws<ArgumentNullException>(() =>
      result.Bind<int>(null!));
    Assert.Equal("binder", exception.ParamName);
  }

  #endregion

  #region Match Operation Tests

  [Fact]
  public void Match_WithSuccessResult_ShouldExecuteOnSuccess() {
    var result = Result.Success(42);

    var matched = result.Match(
      onSuccess: value => $"Success: {value}",
      onFailure: error => $"Error: {error.Message}");

    Assert.Equal("Success: 42", matched);
  }

  [Fact]
  public void Match_WithFailureResult_ShouldExecuteOnFailure() {
    var result = Result.Failure<int>("Test error");

    var matched = result.Match(
      onSuccess: value => $"Success: {value}",
      onFailure: error => $"Error: {error.Message}");

    Assert.Equal("Error: Test error", matched);
  }

  [Fact]
  public void Match_WithNullOnSuccess_ShouldThrowArgumentNullException() {
    var result = Result.Success(42);

    var ex = Assert.Throws<ArgumentNullException>(() =>
      result.Match<string>(null!, e => "Error"));
    Assert.Equal("onSuccess", ex.ParamName);
  }

  [Fact]
  public void Match_WithNullOnFailure_ShouldThrowArgumentNullException() {
    var result = Result.Success(42);

    var ex = Assert.Throws<ArgumentNullException>(() =>
      result.Match<string>(v => "Success", null!));
    Assert.Equal("onFailure", ex.ParamName);
  }

  #endregion

  #region Tap Operations Tests

  [Fact]
  public void Tap_WithSuccessResult_ShouldExecuteAction() {
    var result = Result.Success(42);
    var actionExecuted = false;

    var tappedResult = result.Tap(value => actionExecuted = true);

    Assert.True(actionExecuted);
    Assert.True(tappedResult.IsSuccess);
    Assert.Equal(42, tappedResult.Value);
  }

  [Fact]
  public void Tap_WithFailureResult_ShouldNotExecuteAction() {
    var result = Result.Failure<int>("Error");
    var actionExecuted = false;

    var tappedResult = result.Tap(value => actionExecuted = true);

    Assert.False(actionExecuted);
    Assert.True(tappedResult.IsFailure);
  }

  [Fact]
  public void Tap_WithNullAction_ShouldThrowArgumentNullException() {
    var result = Result.Success(42);

    var ex = Assert.Throws<ArgumentNullException>(() => result.Tap(null!));
    Assert.Equal("action", ex.ParamName);
  }

  [Fact]
  public void TapError_WithFailureResult_ShouldExecuteAction() {
    var error = ResultError.NotFound("TEST", "Test error");
    var result = Result.Failure<int>(error);
    var actionExecuted = false;

    var tappedResult = result.TapError(err => actionExecuted = true);

    Assert.True(actionExecuted);
    Assert.True(tappedResult.IsFailure);
  }

  [Fact]
  public void TapError_WithSuccessResult_ShouldNotExecuteAction() {
    var result = Result.Success(42);
    var actionExecuted = false;

    var tappedResult = result.TapError(err => actionExecuted = true);

    Assert.False(actionExecuted);
    Assert.Equal(result, tappedResult);
    Assert.True(tappedResult.IsSuccess);
  }

  [Fact]
  public void TapError_WithNullAction_ShouldThrowArgumentNullException() {
    var result = Result.Failure<int>("Error");

    var ex = Assert.Throws<ArgumentNullException>(() => result.TapError(null!));
    Assert.Equal("action", ex.ParamName);
  }

  #endregion

  #region Equality Tests

  [Fact]
  public void Equals_WithSameSuccessValues_ShouldReturnTrue() {
    var result1 = Result.Success(42);
    var result2 = Result.Success(42);

    var equals = result1.Equals(result2);

    Assert.True(equals);
    Assert.True(result1 == result2);
    Assert.False(result1 != result2);
  }

  [Fact]
  public void Equals_WithDifferentSuccessValues_ShouldReturnFalse() {
    var result1 = Result.Success(42);
    var result2 = Result.Success(24);

    var equals = result1.Equals(result2);

    Assert.False(equals);
    Assert.False(result1 == result2);
    Assert.True(result1 != result2);
  }

  [Fact]
  public void Equals_WithSameFailureErrors_ShouldReturnTrue() {
    var error = ResultError.NotFound("TEST", "Test error");
    var result1 = Result.Failure<int>(error);
    var result2 = Result.Failure<int>(error);

    var equals = result1.Equals(result2);

    Assert.True(equals);
    Assert.True(result1 == result2);
    Assert.False(result1 != result2);
  }

  [Fact]
  public void Equals_WithSuccessAndFailure_ShouldReturnFalse() {
    var successResult = Result.Success(42);
    var failureResult = Result.Failure<int>("Error");

    var equals = successResult.Equals(failureResult);

    Assert.False(equals);
    Assert.False(successResult == failureResult);
  }

  [Fact]
  public void GetHashCode_WithSameSuccessValues_ShouldReturnSameHashCode() {
    var result1 = Result.Success(42);
    var result2 = Result.Success(42);

    var hash1 = result1.GetHashCode();
    var hash2 = result2.GetHashCode();

    Assert.Equal(hash1, hash2);
  }

  #endregion

  #region Implicit Conversion Tests

  [Fact]
  public void ImplicitConversion_FromValue_ShouldCreateSuccessResult() {
    const int value = 42;

    Result<int> result = value;

    Assert.True(result.IsSuccess);
    Assert.Equal(value, result.Value);
  }

  [Fact]
  public void ImplicitConversion_FromError_ShouldCreateFailureResult() {
    var error = ResultError.NotFound("TEST", "Test error");

    Result<int> result = error;

    Assert.True(result.IsFailure);
    Assert.Equal(error, result.Error);
  }

  #endregion

  #region ToString Tests

  [Fact]
  public void ToString_WithSuccessResult_ShouldReturnSuccessFormat() {
    var result = Result.Success(42);

    var stringResult = result.ToString();

    Assert.Equal("Success(42)", stringResult);
  }

  [Fact]
  public void ToString_WithFailureResult_ShouldReturnFailureFormat() {
    var error = ResultError.NotFound("TEST", "Test error");
    var result = Result.Failure<int>(error);

    var stringResult = result.ToString();

    Assert.Equal($"Failure({error})", stringResult);
  }

  #endregion
}
