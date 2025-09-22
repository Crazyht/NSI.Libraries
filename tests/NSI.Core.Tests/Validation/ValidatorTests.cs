using System.Diagnostics;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Tests.Validation;
/// <summary>
/// Tests for the <see cref="Validator{T}"/> base class.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify synchronous and asynchronous validation execution,
/// rule aggregation, and error collection behavior.
/// </para>
/// </remarks>
public sealed class ValidatorTests {
  private sealed class TestModel {
    public string? Name { get; set; }
    public int Age { get; set; }
  }

  private sealed class AlwaysFailRule: IValidationRule<TestModel> {
    public IEnumerable<IValidationError> Validate(
      TestModel instance,
      IValidationContext context) {
      yield return new ValidationError("ALWAYS_FAIL", "This rule always fails");
    }
  }

  private sealed class AlwaysFailAsyncRule: IAsyncValidationRule<TestModel> {
    public async Task<IEnumerable<IValidationError>> ValidateAsync(
      TestModel instance,
      IValidationContext context,
      CancellationToken cancellationToken = default) {
      await Task.Delay(10, cancellationToken);
      return [
        new ValidationError("ASYNC_FAIL", "Async rule failed")
      ];
    }
  }

  [Fact]
  public void Validate_WithNoRules_ShouldReturnSuccess() {
    var validator = new Validator<TestModel>();
    var model = new TestModel { Name = "Test", Age = 25 };

    var result = validator.Validate(model);

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Validate_WithFailingRule_ShouldReturnErrors() {
    var validator = new Validator<TestModel>()
      .AddRule(new AlwaysFailRule());
    var model = new TestModel();

    var result = validator.Validate(model);

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("ALWAYS_FAIL", result.Errors[0].ErrorCode);
  }

  [Fact]
  public void Validate_WithAsyncRules_ShouldExecuteSynchronously() {
    var validator = new Validator<TestModel>()
      .AddAsyncRule(new AlwaysFailAsyncRule());
    var model = new TestModel();

    var result = validator.Validate(model);

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("ASYNC_FAIL", result.Errors[0].ErrorCode);
  }

  [Fact]
  public void Validate_WithMixedRules_ShouldExecuteAllRules() {
    var validator = new Validator<TestModel>()
      .AddRule(new AlwaysFailRule())
      .AddAsyncRule(new AlwaysFailAsyncRule());
    var model = new TestModel();

    var result = validator.Validate(model);

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Contains(result.Errors, e => e.ErrorCode == "ALWAYS_FAIL");
    Assert.Contains(result.Errors, e => e.ErrorCode == "ASYNC_FAIL");
  }

  [Fact]
  public void Validate_WithNullInstance_ShouldThrowArgumentNullException() {
    var validator = new Validator<TestModel>();

    var exception = Assert.Throws<ArgumentNullException>(
      () => validator.Validate(null!)
    );

    Assert.Equal("instance", exception.ParamName);
  }

  [Fact]
  public async Task ValidateAsync_WithAsyncRules_ShouldExecuteAllRulesConcurrently() {
    var validator = new Validator<TestModel>()
      .AddRule(new AlwaysFailRule())
      .AddAsyncRule(new AlwaysFailAsyncRule());
    var model = new TestModel();

    var result = await validator.ValidateAsync(model);

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(5)]
  [InlineData(10)]
  public async Task ValidateAsync_WithMultipleAsyncRules_ShouldExecuteConcurrently(
    int ruleCount) {
    var validator = new Validator<TestModel>();

    for (var i = 0; i < ruleCount; i++) {
      validator.AddAsyncRule(new AlwaysFailAsyncRule());
    }

    var model = new TestModel();
    var stopwatch = Stopwatch.StartNew();

    var result = await validator.ValidateAsync(model);

    stopwatch.Stop();

    Assert.False(result.IsValid);
    Assert.Equal(ruleCount, result.Errors.Count);
    // Verify concurrent execution - should take roughly the same time as one rule
    Assert.True(stopwatch.ElapsedMilliseconds < 100);
  }
}
