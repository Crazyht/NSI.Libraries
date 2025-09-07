using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

using NSI.Core.Mediator.Abstractions;
using NSI.Core.Mediator.Decorators;
using NSI.Core.Results;
using NSI.Testing.Loggers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NSI.Core.Tests.Mediator.Decorators {
  /// <summary>
  /// Tests for the <see cref="LoggingDecorator{TRequest, TResponse}"/> functionality.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests verify that logging is correctly applied around request processing,
  /// including timing measurements, correlation ID tracking, and appropriate log levels
  /// for different execution scenarios.
  /// </para>
  /// <para>
  /// Test coverage includes:
  /// <list type="bullet">
  ///   <item><description>Successful request processing with timing logs</description></item>
  ///   <item><description>Failed request processing with error details</description></item>
  ///   <item><description>Exception handling and logging</description></item>
  ///   <item><description>Correlation ID extraction and usage</description></item>
  ///   <item><description>Cancellation token handling</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public class LoggingDecoratorTests {
    private readonly InMemoryLogEntryStore _LogEntryStore;
    private readonly ILogger<LoggingDecorator<TestRequest, TestResponse>> _MockLogger;
    private readonly LoggingDecorator<TestRequest, TestResponse> _Decorator;

    public LoggingDecoratorTests() {
      _LogEntryStore = new();
      _MockLogger = new MockLogger<LoggingDecorator<TestRequest, TestResponse>>(_LogEntryStore);
      _Decorator = new LoggingDecorator<TestRequest, TestResponse>(_MockLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException() {
      // Act & Assert
      var ex = Assert.Throws<ArgumentNullException>(() =>
        new LoggingDecorator<TestRequest, TestResponse>(null!));

      Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance() {
      // Act
      var decorator = new LoggingDecorator<TestRequest, TestResponse>(_MockLogger);

      // Assert
      Assert.NotNull(decorator);
    }

    #endregion

    #region HandleAsync Parameter Validation

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException() {
      // Arrange
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      // Act & Assert
      var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
        _Decorator.HandleAsync(null!, mockContinuation, CancellationToken.None));

      Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithNullContinuation_ShouldThrowArgumentNullException() {
      // Arrange
      var request = new TestRequest("test-data");

      // Act & Assert
      var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
        _Decorator.HandleAsync(request, null!, CancellationToken.None));

      Assert.Equal("continuation", ex.ParamName);
    }

    #endregion

    #region Successful Execution Tests

    [Fact]
    public async Task HandleAsync_WithSuccessfulExecution_ShouldLogStartAndSuccess() {
      // Arrange
      var request = new TestRequest("test-data");
      var expectedResponse = new TestResponse("processed-data");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

      // Act
      var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.Equal(expectedResponse.ProcessedData, result.Value.ProcessedData);

      // Verify logging was called
      var logEntries = _LogEntryStore.GetAll();
      Assert.NotEmpty(logEntries);
      Assert.NotEmpty(logEntries.WithMessageContains("Starting request processing: TestRequest").WithLogLevel(LogLevel.Information));
      Assert.NotEmpty(logEntries.WithMessageContains("Request TestRequest completed successfully").WithLogLevel(LogLevel.Information));
    }

    [Fact]
    public async Task HandleAsync_WithCorrelatedRequest_ShouldLogCorrelationId() {
      // Arrange
      var correlationId = "test-correlation-123";
      var request = new CorrelatedTestRequest("test-data", correlationId);
      var expectedResponse = new TestResponse("processed-data");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      mockContinuation.Invoke().Returns(Result.Success(expectedResponse));
      var mockLogger = new MockLogger<LoggingDecorator<CorrelatedTestRequest, TestResponse>>(_LogEntryStore);
      // Create decorator for correlated request
      var correlatedDecorator = new LoggingDecorator<CorrelatedTestRequest, TestResponse>(mockLogger);

      // Act
      var result = await correlatedDecorator.HandleAsync(request, mockContinuation, CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);

      // Verify correlation ID is logged
      var logEntries = _LogEntryStore.GetAll();
      Assert.NotEmpty(logEntries);
      Assert.NotEmpty(logEntries.WithMessageContains(correlationId).WithLogLevel(LogLevel.Information));
    }

    #endregion

    #region Failed Execution Tests

    [Fact]
    public async Task HandleAsync_WithFailedResult_ShouldLogFailure() {
      // Arrange
      var request = new TestRequest("test-data");
      var error = ResultError.BusinessRule("BUSINESS_ERROR", "Something went wrong");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      mockContinuation.Invoke().Returns(Result.Failure<TestResponse>(error));

      // Act
      var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

      // Assert
      Assert.True(result.IsFailure);
      Assert.Equal(error.Code, result.Error.Code);

      // Verify failure logging
      var logEntries = _LogEntryStore.GetAll();
      Assert.NotEmpty(logEntries);
      Assert.NotEmpty(logEntries.WithMessageContains("Request TestRequest failed").WithMessageContains("BusinessRule").WithLogLevel(LogLevel.Warning));
    }

    [Fact]
    public async Task HandleAsync_WithException_ShouldLogErrorAndRethrow() {
      // Arrange
      var request = new TestRequest("test-data");
      var expectedException = new InvalidOperationException("Handler failed");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      mockContinuation.Invoke().Throws(expectedException);

      // Act & Assert
      var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None));

      Assert.Equal(expectedException, thrownException);

      // Verify error logging
      var logEntries = _LogEntryStore.GetAll();
      Assert.NotEmpty(logEntries);
      var logEntry = logEntries.WithMessageContains("Request TestRequest threw an exception").WithLogLevel(LogLevel.Error).WithException().FirstOrDefault();
      Assert.NotNull(logEntry);
      Assert.Equal(expectedException, logEntry.Exception);
    }

    [Fact]
    public async Task HandleAsync_WithOperationCancelledException_ShouldLogCancellationAndRethrow() {
      // Arrange
      var request = new TestRequest("test-data");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      mockContinuation.Invoke().Throws(new OperationCanceledException());

      // Act & Assert
      await Assert.ThrowsAsync<OperationCanceledException>(() =>
        _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None));

      // Verify cancellation logging
      var logEntries = _LogEntryStore.GetAll();
      Assert.NotEmpty(logEntries);
      Assert.NotEmpty(logEntries.WithMessageContains("Request TestRequest was cancelled").WithLogLevel(LogLevel.Warning));
    }

    #endregion

    #region Timing Tests

    [Fact]
    public async Task HandleAsync_ShouldMeasureExecutionTime() {
      // Arrange
      var request = new TestRequest("test-data");
      var expectedResponse = new TestResponse("processed-data");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();


      async Task<Result<TestResponse>> MockContinuationTask() {
        await Task.Delay(100); // Simulate some processing time
        return Result.Success(expectedResponse);
      }

      mockContinuation.Invoke().Returns(MockContinuationTask());

      // Act
      var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);

      // Verify timing information is logged
      var logEntries = _LogEntryStore.GetAll();
      Assert.NotEmpty(logEntries);
      Assert.NotEmpty(logEntries.WithMessageContains("completed successfully").WithMessageContains("ms").WithLogLevel(LogLevel.Information));
    }

    #endregion

    #region Scope Tests

    [Fact]
    public async Task HandleAsync_ShouldCreateLoggingScope() {
      // Arrange
      var request = new TestRequest("test-data");
      var expectedResponse = new TestResponse("processed-data");
      var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

      mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

      // Act
      await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

      // Assert
      // Verify that BeginScope was called (this verifies the using statement creates a scope)
      var logs = _LogEntryStore.GetAll().WithScopeContainingKey("RequestType");
      Assert.NotEmpty(logs);
    }

    #endregion
  }

  #region Test Helper Types

  /// <summary>
  /// Test request for logging validation.
  /// </summary>
  [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Needed for Proxy code.")]
  public record TestRequest(string Data): IQuery<TestResponse>;

  /// <summary>
  /// Test request with correlation ID support.
  /// </summary>
  [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Needed for Proxy code.")]
  public record CorrelatedTestRequest(string Data, string? CorrelationId): IQuery<TestResponse>, ICorrelatedRequest;

  /// <summary>
  /// Test response for logging validation.
  /// </summary>
  [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Needed for Proxy code.")]
  public record TestResponse(string ProcessedData);

  #endregion
}
