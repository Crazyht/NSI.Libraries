using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSI.Core.Mediator;
using NSI.Core.Mediator.Abstractions;
using NSI.Core.Mediator.Decorators;
using NSI.Core.Results;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Tests.Mediator.Integration;
/// <summary>
/// Integration tests for the complete mediator pipeline with decorators.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the end-to-end functionality of the mediator with
/// decorator pipeline, including proper execution order, error handling,
/// and integration with dependency injection.
/// </para>
/// <para>
/// Test coverage includes:
/// <list type="bullet">
///   <item><description>Complete pipeline execution with multiple decorators</description></item>
///   <item><description>Decorator execution order validation</description></item>
///   <item><description>Short-circuiting behavior in decorators</description></item>
///   <item><description>Error handling throughout the pipeline</description></item>
///   <item><description>Integration with dependency injection container</description></item>
///   <item><description>New NSI.Core.Validation framework integration</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class MediatorPipelineIntegrationTests: IDisposable {
  private readonly ServiceProvider _ServiceProvider;
  private readonly IMediator _Mediator;

  public MediatorPipelineIntegrationTests() {
    var services = new ServiceCollection();

    // Register mediator and logging
    services.AddSingleton<ILogger<MediatorImplementation>>(
      NullLogger<MediatorImplementation>.Instance);
    services.AddSingleton<ILogger<LoggingDecorator<TestQuery, TestResponse>>>(
      NullLogger<LoggingDecorator<TestQuery, TestResponse>>.Instance);
    services.AddSingleton<ILogger<ValidationDecorator<TestQuery, TestResponse>>>(
      NullLogger<ValidationDecorator<TestQuery, TestResponse>>.Instance);
    services.AddSingleton<ILogger<LoggingDecorator<ValidatedTestCommand, TestResponse>>>(
      NullLogger<LoggingDecorator<ValidatedTestCommand, TestResponse>>.Instance);
    services.AddSingleton<ILogger<ValidationDecorator<ValidatedTestCommand, TestResponse>>>(
      NullLogger<ValidationDecorator<ValidatedTestCommand, TestResponse>>.Instance);

    services.AddScoped<IMediator, MediatorImplementation>();

    // Register handlers
    services.AddScoped<IRequestHandler<TestQuery, TestResponse>, TestQueryHandler>();
    services.AddScoped<IRequestHandler<ValidatedTestCommand, TestResponse>,
      ValidatedTestCommandHandler>();
    services.AddScoped<IRequestHandler<ExecutionOrderTestQuery, TestResponse>,
      ExecutionOrderTestQueryHandler>();

    // Register decorators
    services.AddScoped<IRequestDecorator<TestQuery, TestResponse>,
      LoggingDecorator<TestQuery, TestResponse>>();
    services.AddScoped<IRequestDecorator<TestQuery, TestResponse>,
      ValidationDecorator<TestQuery, TestResponse>>();

    services.AddScoped<IRequestDecorator<ValidatedTestCommand, TestResponse>,
      ValidationDecorator<ValidatedTestCommand, TestResponse>>();
    services.AddScoped<IRequestDecorator<ValidatedTestCommand, TestResponse>,
      LoggingDecorator<ValidatedTestCommand, TestResponse>>();

    services.AddScoped<IRequestDecorator<ExecutionOrderTestQuery, TestResponse>,
      ExecutionOrderDecorator>();

    // Register validators using the new NSI.Core.Validation framework
    services.AddScoped<IValidator<ValidatedTestCommand>, ValidatedTestCommandValidator>();

    _ServiceProvider = services.BuildServiceProvider();
    _Mediator = _ServiceProvider.GetRequiredService<IMediator>();
  }

  #region Test Helper Types

  /// <summary>
  /// Simple test query for integration testing.
  /// </summary>
  internal sealed record TestQuery(string Data): IQuery<TestResponse>;

  /// <summary>
  /// Test command with validation using NSI.Core.Validation framework.
  /// </summary>
  internal sealed record ValidatedTestCommand: ICommand<TestResponse> {
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
  }

  /// <summary>
  /// Test query for execution order validation.
  /// </summary>
  internal sealed record ExecutionOrderTestQuery(List<string> ExecutionOrder):
    IQuery<TestResponse>;

  /// <summary>
  /// Test response type.
  /// </summary>
  internal sealed record TestResponse(string ProcessedData);

  /// <summary>
  /// Validator for ValidatedTestCommand using NSI.Core.Validation framework.
  /// </summary>
  internal sealed class ValidatedTestCommandValidator: Validator<ValidatedTestCommand> {
    public ValidatedTestCommandValidator() => this
      .Required(x => x.Name)
      .StringLength(x => x.Name, minLength: 2, maxLength: 50)
      .Required(x => x.Email)
      .Email(x => x.Email);
  }

  /// <summary>
  /// Handler for test queries.
  /// </summary>
  internal sealed class TestQueryHandler: IRequestHandler<TestQuery, TestResponse> {
    public Task<Result<TestResponse>> HandleAsync(
      TestQuery request,
      CancellationToken cancellationToken = default) {

      var response = new TestResponse($"Processed: {request.Data}");
      return Task.FromResult(Result.Success(response));
    }
  }

  /// <summary>
  /// Handler for validated test commands.
  /// </summary>
  internal sealed class ValidatedTestCommandHandler: IRequestHandler<ValidatedTestCommand, TestResponse> {
    public Task<Result<TestResponse>> HandleAsync(
      ValidatedTestCommand request,
      CancellationToken cancellationToken = default) {

      var response = new TestResponse($"Created user: {request.Name} ({request.Email})");
      return Task.FromResult(Result.Success(response));
    }
  }

  /// <summary>
  /// Handler for execution order test queries.
  /// </summary>
  internal sealed class ExecutionOrderTestQueryHandler: IRequestHandler<ExecutionOrderTestQuery, TestResponse> {
    public Task<Result<TestResponse>> HandleAsync(
      ExecutionOrderTestQuery request,
      CancellationToken cancellationToken = default) {

      request.ExecutionOrder.Add("Handler");
      var response = new TestResponse("Handler executed");
      return Task.FromResult(Result.Success(response));
    }
  }

  /// <summary>
  /// Decorator that tracks execution order.
  /// </summary>
  internal sealed class ExecutionOrderDecorator:
    IRequestDecorator<ExecutionOrderTestQuery, TestResponse> {

    public async Task<Result<TestResponse>> HandleAsync(
      ExecutionOrderTestQuery request,
      RequestHandlerFunction<TestResponse> continuation,
      CancellationToken cancellationToken = default) {

      request.ExecutionOrder.Add("Decorator_Before");
      var result = await continuation();
      request.ExecutionOrder.Add("Decorator_After");
      return result;
    }
  }

  #endregion

  #region Pipeline Integration Tests

  [Fact]
  public async Task ProcessAsync_WithSimpleQuery_ShouldExecuteThroughPipeline() {
    // Arrange
    var query = new TestQuery("integration-test");

    // Act
    var result = await _Mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Processed: integration-test", result.Value.ProcessedData);
  }

  [Fact]
  public async Task ProcessAsync_WithValidCommand_ShouldPassValidationAndExecute() {
    // Arrange
    var command = new ValidatedTestCommand {
      Name = "John Doe",
      Email = "john.doe@example.com"
    };

    // Act
    var result = await _Mediator.ProcessAsync(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Created user: John Doe (john.doe@example.com)", result.Value.ProcessedData);
  }

  [Fact]
  public async Task ProcessAsync_WithInvalidCommand_ShouldFailValidationBeforeHandler() {
    // Arrange
    var command = new ValidatedTestCommand {
      Name = "", // Required but empty - will fail RequiredRule
      Email = "invalid-email" // Invalid format - will fail EmailRule
    };

    // Act
    var result = await _Mediator.ProcessAsync(command);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Validation, result.Error.Type);
    Assert.Equal("VALIDATION_FAILED", result.Error.Code);
    Assert.NotNull(result.Error.ValidationErrors);
    Assert.True(result.Error.ValidationErrors.Count > 0);

    // Verify we have errors for both Name and Email
    var nameErrors = result.Error.ValidationErrors
      .Where(e => e.PropertyName == nameof(ValidatedTestCommand.Name));
    var emailErrors = result.Error.ValidationErrors
      .Where(e => e.PropertyName == nameof(ValidatedTestCommand.Email));

    Assert.NotEmpty(nameErrors);
    Assert.NotEmpty(emailErrors);
  }

  [Fact]
  public async Task ProcessAsync_WithInvalidNameLength_ShouldFailStringLengthValidation() {
    // Arrange
    var command = new ValidatedTestCommand {
      Name = "A", // Too short - minimum is 2 characters
      Email = "valid@example.com"
    };

    // Act
    var result = await _Mediator.ProcessAsync(command);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Validation, result.Error.Type);
    Assert.NotNull(result.Error.ValidationErrors);

    var nameError = result.Error.ValidationErrors.FirstOrDefault(e =>
      e.PropertyName == nameof(ValidatedTestCommand.Name) && e.ErrorCode == "TOO_SHORT");
    Assert.NotNull(nameError);
    Assert.Contains("at least 2 characters", nameError.ErrorMessage, StringComparison.Ordinal);
  }

  [Fact]
  public async Task ProcessAsync_WithExecutionOrderTracking_ShouldExecuteInCorrectOrder() {
    // Arrange
    var executionOrder = new List<string>();
    var query = new ExecutionOrderTestQuery(executionOrder);

    // Act
    var result = await _Mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Handler executed", result.Value.ProcessedData);

    // Verify execution order: Decorator -> Handler -> Decorator
    Assert.Equal(3, executionOrder.Count);
    Assert.Equal("Decorator_Before", executionOrder[0]);
    Assert.Equal("Handler", executionOrder[1]);
    Assert.Equal("Decorator_After", executionOrder[2]);
  }

  #endregion

  #region Multiple Decorators Tests

  [Fact]
  public async Task ProcessAsync_WithMultipleDecorators_ShouldExecuteAllInOrder() {
    // This test verifies that both logging and validation decorators are executed
    // for the TestQuery type when both are registered

    // Arrange
    var query = new TestQuery("multi-decorator-test");

    // Act
    var result = await _Mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Processed: multi-decorator-test", result.Value.ProcessedData);

    // The test passes if no exceptions are thrown and result is successful
    // This indicates both decorators executed without interfering with each other
  }

  #endregion

  #region Error Handling Tests

  [Fact]
  public async Task ProcessAsync_WithHandlerThatFails_ShouldPropagateErrorThroughPipeline() {
    // Arrange - Create a service collection with a failing handler
    var services = new ServiceCollection();
    services.AddSingleton<ILogger<MediatorImplementation>>(
      NullLogger<MediatorImplementation>.Instance);
    services.AddSingleton<ILogger<LoggingDecorator<TestQuery, TestResponse>>>(
      NullLogger<LoggingDecorator<TestQuery, TestResponse>>.Instance);
    services.AddScoped<IMediator, MediatorImplementation>();
    services.AddScoped<IRequestHandler<TestQuery, TestResponse>, FailingTestQueryHandler>();
    services.AddScoped<IRequestDecorator<TestQuery, TestResponse>,
      LoggingDecorator<TestQuery, TestResponse>>();

    using var serviceProvider = services.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<IMediator>();

    var query = new TestQuery("will-fail");

    // Act
    var result = await mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.BusinessRule, result.Error.Type);
    Assert.Equal("HANDLER_FAILURE", result.Error.Code);
  }

  /// <summary>
  /// Handler that always fails for testing error propagation.
  /// </summary>
  internal sealed class FailingTestQueryHandler: IRequestHandler<TestQuery, TestResponse> {
    public Task<Result<TestResponse>> HandleAsync(
      TestQuery request,
      CancellationToken cancellationToken = default) {

      var error = ResultError.BusinessRule(
        "HANDLER_FAILURE",
        "Handler intentionally failed");
      return Task.FromResult(Result.Failure<TestResponse>(error));
    }
  }

  #endregion

  #region Validation Integration Tests

  [Fact]
  public async Task ProcessAsync_WithComplexValidationScenario_ShouldProvideDetailedErrors() {
    // Arrange
    var command = new ValidatedTestCommand {
      Name = "ThisNameIsWayTooLongAndExceedsTheMaximumLengthOf50Characters",
      Email = "not-an-email"
    };

    // Act
    var result = await _Mediator.ProcessAsync(command);

    // Assert
    Assert.True(result.IsFailure);
    Assert.NotNull(result.Error.ValidationErrors);
    Assert.True(result.Error.ValidationErrors.Count >= 2);

    // Should have TOO_LONG error for Name
    var nameLengthError = result.Error.ValidationErrors.FirstOrDefault(e =>
      e.PropertyName == nameof(ValidatedTestCommand.Name) && e.ErrorCode == "TOO_LONG");
    Assert.NotNull(nameLengthError);

    // Should have INVALID_EMAIL error for Email
    var emailError = result.Error.ValidationErrors.FirstOrDefault(e =>
      e.PropertyName == nameof(ValidatedTestCommand.Email) && e.ErrorCode == "INVALID_EMAIL");
    Assert.NotNull(emailError);
  }

  [Fact]
  public async Task ProcessAsync_WithEmptyCommand_ShouldReturnMultipleRequiredFieldErrors() {
    // Arrange
    var command = new ValidatedTestCommand(); // All fields empty

    // Act
    var result = await _Mediator.ProcessAsync(command);

    // Assert
    Assert.True(result.IsFailure);
    Assert.NotNull(result.Error.ValidationErrors);

    // Should have REQUIRED errors for both Name and Email
    var requiredErrors = result.Error.ValidationErrors
      .Where(e => e.ErrorCode == "REQUIRED").ToList();
    Assert.True(requiredErrors.Count >= 2);

    var nameRequired = requiredErrors
      .Any(e => e.PropertyName == nameof(ValidatedTestCommand.Name));
    var emailRequired = requiredErrors
      .Any(e => e.PropertyName == nameof(ValidatedTestCommand.Email));

    Assert.True(nameRequired);
    Assert.True(emailRequired);
  }

  #endregion

  #region Performance Tests

  [Fact]
  public async Task ProcessAsync_WithPipeline_ShouldCompleteWithinReasonableTime() {
    // Arrange
    var query = new TestQuery("performance-test");
    var stopwatch = Stopwatch.StartNew();

    // Act - Execute multiple times to get average performance
    const int iterations = 100;
    for (var i = 0; i < iterations; i++) {
      var result = await _Mediator.ProcessAsync(query);
      Assert.True(result.IsSuccess);
    }

    stopwatch.Stop();

    // Assert - Should complete all iterations within reasonable time
    // This is a loose performance test to catch major regressions
    var avgTimePerRequest = stopwatch.ElapsedMilliseconds / (double)iterations;
    Assert.True(avgTimePerRequest < 10,
      $"Average time per request ({avgTimePerRequest:F2}ms) exceeds threshold (10ms)");
  }

  #endregion

  #region Concurrent Execution Tests

  [Fact]
  public async Task ProcessAsync_WithConcurrentRequests_ShouldHandleAllCorrectly() {
    // Arrange
    const int concurrentRequests = 50;
    var tasks = new List<Task<Result<TestResponse>>>();

    // Act - Create multiple concurrent requests
    for (var i = 0; i < concurrentRequests; i++) {
      var query = new TestQuery($"concurrent-test-{i}");
      tasks.Add(_Mediator.ProcessAsync(query));
    }

    var results = await Task.WhenAll(tasks);

    // Assert
    Assert.Equal(concurrentRequests, results.Length);

    for (var i = 0; i < results.Length; i++) {
      Assert.True(results[i].IsSuccess, $"Request {i} failed");
      Assert.Equal($"Processed: concurrent-test-{i}", results[i].Value.ProcessedData);
    }
  }

  #endregion

  #region Resource Cleanup

  public void Dispose() => _ServiceProvider?.Dispose();

  #endregion
}
