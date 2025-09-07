using System.Globalization;
using NSI.Core.Results;

namespace NSI.Core.Tests.Results;
/// <summary>
/// Tests for the static methods in the <see cref="Result"/> class.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the static factory methods and utility functions
/// for creating and combining Result instances.
/// </para>
/// </remarks>
public class ResultStaticTests {
  #region Try Tests

  [Fact]
  public void Try_WithSuccessfulOperation_ShouldReturnSuccess() {
    var result = Result.Try(() => int.Parse("42", NumberStyles.Integer, CultureInfo.InvariantCulture));

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void Try_WithExceptionThrowingOperation_ShouldReturnFailure() {
    var result = Result.Try(() => int.Parse("invalid", NumberStyles.Integer, CultureInfo.InvariantCulture));

    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Generic, result.Error.Type);
    Assert.Equal("EXCEPTION", result.Error.Code);
    Assert.NotNull(result.Error.Exception);
    Assert.IsType<FormatException>(result.Error.Exception);
  }

  [Fact]
  public void Try_WithNullOperation_ShouldThrowArgumentNullException() {

    var exception = Assert.Throws<ArgumentNullException>(() => Result.Try<int>(null!));
    Assert.Equal("operation", exception.ParamName);
  }

  #endregion

  #region TryAsync Tests

  [Fact]
  public async Task TryAsync_WithSuccessfulOperation_ShouldReturnSuccess() {
    var result = await Result.TryAsync(() => Task.FromResult(42));

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public async Task TryAsync_WithExceptionThrowingOperation_ShouldReturnFailure() {
    var result = await Result.TryAsync<int>(() => throw new InvalidOperationException("Test exception"));

    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Generic, result.Error.Type);
    Assert.Equal("EXCEPTION", result.Error.Code);
    Assert.NotNull(result.Error.Exception);
    Assert.IsType<InvalidOperationException>(result.Error.Exception);
  }

  [Fact]
  public async Task TryAsync_WithNullOperation_ShouldThrowArgumentNullException() {
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await Result.TryAsync<int>(null!));
    Assert.Equal("operation", exception.ParamName);
  }

  #endregion

  #region Combine Tests

  [Fact]
  public void Combine_WithAllSuccessResults_ShouldReturnSuccessWithArray() {
    var result1 = Result.Success(1);
    var result2 = Result.Success(2);
    var result3 = Result.Success(3);

    var combined = Result.Combine(result1, result2, result3);

    Assert.True(combined.IsSuccess);
    Assert.Equal([1, 2, 3], combined.Value);
  }

  [Fact]
  public void Combine_WithOneFailureResult_ShouldReturnFirstFailure() {
    var result1 = Result.Success(1);
    var failureResult = Result.Failure<int>(ResultError.NotFound("ERROR", "Error"));
    var result3 = Result.Success(3);

    var combined = Result.Combine(result1, failureResult, result3);

    Assert.True(combined.IsFailure);
    Assert.Equal(failureResult.Error, combined.Error);
  }

  [Fact]
  public void Combine_WithMultipleFailures_ShouldReturnFirstFailure() {
    var firstError = ResultError.NotFound("FIRST", "First error");
    var secondError = ResultError.Conflict("SECOND", "Second error");
    var result1 = Result.Failure<int>(firstError);
    var result2 = Result.Failure<int>(secondError);

    var combined = Result.Combine(result1, result2);

    Assert.True(combined.IsFailure);
    Assert.Equal(firstError, combined.Error);
  }

  [Fact]
  public void Combine_WithNullResults_ShouldThrowArgumentNullException() {
    var exception = Assert.Throws<ArgumentNullException>(() => Result.Combine<int>((Result<int>[])null!));
    Assert.Equal("results", exception.ParamName);
  }

  [Fact]
  public void Combine_WithEnumerable_WithAllSuccessResults_ShouldReturnSuccessWithList() {
    var results = new List<Result<int>> {
      Result.Success(1),
      Result.Success(2),
      Result.Success(3)
    };

    var combined = Result.Combine(results);

    Assert.True(combined.IsSuccess);
    Assert.Equal([1, 2, 3], combined.Value);
  }

  [Fact]
  public void Combine_WithEnumerable_WithOneFailure_ShouldReturnFirstFailure() {
    var error = ResultError.NotFound("ERROR", "Error");
    var results = new List<Result<int>> {
      Result.Success(1),
      Result.Failure<int>(error),
      Result.Success(3)
    };

    var combined = Result.Combine(results);

    Assert.True(combined.IsFailure);
    Assert.Equal(error, combined.Error);
  }

  [Fact]
  public void Combine_WithNullEnumerable_ShouldThrowArgumentNullException() {
    IEnumerable<Result<int>>? nullResults = null;
    var exception = Assert.Throws<ArgumentNullException>(() => Result.Combine(nullResults!));
    Assert.Equal("results", exception.ParamName);
  }

  #endregion
}
