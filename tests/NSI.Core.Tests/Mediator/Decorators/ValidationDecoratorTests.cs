using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSI.Core.Mediator.Abstractions;
using NSI.Core.Mediator.Decorators;
using NSI.Core.Results;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NSI.Core.Tests.Mediator.Decorators;
/// <summary>
/// Tests for the <see cref="ValidationDecorator{TRequest, TResponse}"/> functionality.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that validation is correctly applied to requests through the decorator pattern,
/// including validation execution, error handling, and integration with the validation framework.
/// </para>
/// <para>
/// Test coverage includes:
/// <list type="bullet">
///   <item><description>Successful validation with continuation to next handler</description></item>
///   <item><description>Failed validation with error collection and short-circuiting</description></item>
///   <item><description>Missing validator scenarios (graceful handling)</description></item>
///   <item><description>Validation context creation and service provider integration</description></item>
///   <item><description>Cancellation token propagation during validation</description></item>
/// </list>
/// </para>
/// </remarks>
public class ValidationDecoratorTests {
  private readonly IServiceProvider _MockServiceProvider;
  private readonly ILogger<ValidationDecorator<TestRequest, TestResponse>> _MockLogger;
  private readonly ValidationDecorator<TestRequest, TestResponse> _Decorator;

  public ValidationDecoratorTests() {
    _MockServiceProvider = Substitute.For<IServiceProvider>();
    _MockLogger = NullLogger<ValidationDecorator<TestRequest, TestResponse>>.Instance;
    _Decorator = new ValidationDecorator<TestRequest, TestResponse>(_MockServiceProvider, _MockLogger);
  }

  #region Test Helper Types

  /// <summary>
  /// Test request for validation testing.
  /// </summary>
  [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Need to be public for NSubstitute.")]
  public record TestRequest(string Name, string Email): IQuery<TestResponse>;

  /// <summary>
  /// Test response for validation testing.
  /// </summary>
  [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Need to be public for NSubstitute.")]
  public record TestResponse(string ProcessedData);

  /// <summary>
  /// Invalid test request for validation failure scenarios.
  /// </summary>
  [SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Need to be public for NSubstitute.")]
  public record InvalidTestRequest(string? Name, string? Email): IQuery<TestResponse>;

  #endregion

  #region Constructor Tests

  [Fact]
  public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException() {
    // Act & Assert
    var ex = Assert.Throws<ArgumentNullException>(() =>
      new ValidationDecorator<TestRequest, TestResponse>(null!, _MockLogger));

    Assert.Equal("serviceProvider", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithNullLogger_ShouldThrowArgumentNullException() {
    // Act & Assert
    var ex = Assert.Throws<ArgumentNullException>(() =>
      new ValidationDecorator<TestRequest, TestResponse>(_MockServiceProvider, null!));

    Assert.Equal("logger", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithValidParameters_ShouldCreateInstance() {
    // Act
    var decorator = new ValidationDecorator<TestRequest, TestResponse>(_MockServiceProvider, _MockLogger);

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
    var request = new TestRequest("John Doe", "john@example.com");

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
      _Decorator.HandleAsync(request, null!, CancellationToken.None));

    Assert.Equal("continuation", ex.ParamName);
  }

  #endregion

  #region Validation Success Tests

  [Fact]
  public async Task HandleAsync_WithNoValidatorRegistered_ShouldSkipValidationAndContinue() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var expectedResponse = new TestResponse("processed");
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns((IValidator<TestRequest>?)null);
    mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

    // Act
    var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedResponse.ProcessedData, result.Value.ProcessedData);

    await mockContinuation.Received(1).Invoke();
    _MockServiceProvider.Received(1).GetService<IValidator<TestRequest>>();
  }

  [Fact]
  public async Task HandleAsync_WithValidRequest_ShouldPassValidationAndContinue() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var expectedResponse = new TestResponse("processed");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    var validationResult = ValidationResult.Success;
    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Returns(validationResult);

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);
    mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

    // Act
    var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedResponse.ProcessedData, result.Value.ProcessedData);

    await mockValidator.Received(1).ValidateAsync(
      request,
      Arg.Any<IValidationContext>(),
      Arg.Any<CancellationToken>());
    await mockContinuation.Received(1).Invoke();
  }

  [Fact]
  public async Task HandleAsync_WithValidRequest_ShouldCreateValidationContextWithServiceProvider() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var expectedResponse = new TestResponse("processed");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    mockValidator.ValidateAsync(
        Arg.Any<TestRequest>(),
        Arg.Do<IValidationContext>(ctx =>             // Verify that ValidationContext is created with our service provider
          Assert.Equal(_MockServiceProvider, ctx.ServiceProvider)),
        Arg.Any<CancellationToken>())
      .Returns(ValidationResult.Success);

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);
    mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

    // Act
    var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
  }

  [Fact]
  public async Task HandleAsync_WithCancellationToken_ShouldPropagateCancellationToValidator() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var expectedResponse = new TestResponse("processed");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();
    var cancellationToken = new CancellationToken();

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), cancellationToken)
      .Returns(ValidationResult.Success);

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);
    mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

    // Act
    var result = await _Decorator.HandleAsync(request, mockContinuation, cancellationToken);

    // Assert
    Assert.True(result.IsSuccess);
    await mockValidator.Received(1).ValidateAsync(request, Arg.Any<IValidationContext>(), cancellationToken);
  }

  #endregion

  #region Validation Failure Tests

  [Fact]
  public async Task HandleAsync_WithValidationFailure_ShouldReturnValidationErrorWithoutContinuation() {
    // Arrange
    var request = new InvalidTestRequest(null, "invalid-email");
    var mockValidator = Substitute.For<IValidator<InvalidTestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    var validationErrors = new List<IValidationError> {
      new ValidationError("REQUIRED", "Name is required.", "Name"),
      new ValidationError("INVALID_EMAIL", "Email format is invalid.", "Email")
    };
    var validationResult = new ValidationResult(validationErrors);

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Returns(validationResult);

    _MockServiceProvider.GetService<IValidator<InvalidTestRequest>>().Returns(mockValidator);

    var mockLogger = NullLogger<ValidationDecorator<InvalidTestRequest, TestResponse>>.Instance;
    var decorator = new ValidationDecorator<InvalidTestRequest, TestResponse>(_MockServiceProvider, mockLogger);

    // Act
    var result = await decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Validation, result.Error.Type);
    Assert.Equal("VALIDATION_FAILED", result.Error.Code);
    Assert.Equal("Request validation failed", result.Error.Message);
    Assert.NotNull(result.Error.ValidationErrors);
    Assert.Equal(2, result.Error.ValidationErrors.Count);

    // Verify continuation was not called (short-circuited)
    await mockContinuation.DidNotReceive().Invoke();
  }

  [Fact]
  public async Task HandleAsync_WithSingleValidationError_ShouldReturnCorrectErrorDetails() {
    // Arrange
    var request = new InvalidTestRequest("", "valid@email.com");
    var mockValidator = Substitute.For<IValidator<InvalidTestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    var validationError = new ValidationError("REQUIRED", "Name is required.", "Name");
    var validationResult = new ValidationResult([validationError]);

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Returns(validationResult);

    _MockServiceProvider.GetService<IValidator<InvalidTestRequest>>().Returns(mockValidator);

    var mockLogger = NullLogger<ValidationDecorator<InvalidTestRequest, TestResponse>>.Instance;
    var decorator = new ValidationDecorator<InvalidTestRequest, TestResponse>(_MockServiceProvider, mockLogger);

    // Act
    var result = await decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Single(result.Error.ValidationErrors!);

    var returnedError = result.Error.ValidationErrors![0];
    Assert.Equal("REQUIRED", returnedError.ErrorCode);
    Assert.Equal("Name is required.", returnedError.ErrorMessage);
    Assert.Equal("Name", returnedError.PropertyName);
  }

  [Fact]
  public async Task HandleAsync_WithValidationFailure_ShouldAggregateErrorMessages() {
    // Arrange
    var request = new InvalidTestRequest(null, "invalid-email");
    var mockValidator = Substitute.For<IValidator<InvalidTestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    var validationErrors = new List<IValidationError> {
      new ValidationError("REQUIRED", "Name is required.", "Name"),
      new ValidationError("INVALID_EMAIL", "Email format is invalid.", "Email"),
      new ValidationError("TOO_SHORT", "Description is too short.", "Description")
    };
    var validationResult = new ValidationResult(validationErrors);

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Returns(validationResult);

    _MockServiceProvider.GetService<IValidator<InvalidTestRequest>>().Returns(mockValidator);

    var mockLogger = NullLogger<ValidationDecorator<InvalidTestRequest, TestResponse>>.Instance;
    var decorator = new ValidationDecorator<InvalidTestRequest, TestResponse>(_MockServiceProvider, mockLogger);

    // Act
    var result = await decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(3, result.Error.ValidationErrors!.Count);

    // Verify all errors are preserved
    var errorCodes = result.Error.ValidationErrors!.Select(e => e.ErrorCode).ToList();
    Assert.Contains("REQUIRED", errorCodes);
    Assert.Contains("INVALID_EMAIL", errorCodes);
    Assert.Contains("TOO_SHORT", errorCodes);
  }

  #endregion

  #region Edge Cases Tests

  [Fact]
  public async Task HandleAsync_WithValidatorThrowingException_ShouldPropagateException() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();
    var expectedException = new InvalidOperationException("Validator failed");

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Throws(expectedException);

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);

    // Act & Assert
    var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None));

    Assert.Equal(expectedException, thrownException);
    await mockContinuation.DidNotReceive().Invoke();
  }

  [Fact]
  public async Task HandleAsync_WithCancelledValidation_ShouldThrowOperationCancelledException() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Throws(new OperationCanceledException());

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(() =>
      _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None));

    await mockContinuation.DidNotReceive().Invoke();
  }

  [Fact]
  public async Task HandleAsync_WithNullValidationResult_ShouldHandleGracefully() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Returns((IValidationResult?)null!);

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);

    // Act & Assert
    // This should throw a NullReferenceException when accessing result.IsValid
    await Assert.ThrowsAsync<NullReferenceException>(() =>
      _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None));
  }

  [Fact]
  public async Task HandleAsync_WithEmptyValidationErrors_ShouldTreatAsValid() {
    // Arrange
    var request = new TestRequest("John Doe", "john@example.com");
    var expectedResponse = new TestResponse("processed");
    var mockValidator = Substitute.For<IValidator<TestRequest>>();
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    var validationResult = new ValidationResult([]);
    mockValidator.ValidateAsync(request, Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
      .Returns(validationResult);

    _MockServiceProvider.GetService<IValidator<TestRequest>>().Returns(mockValidator);
    mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

    // Act
    var result = await _Decorator.HandleAsync(request, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedResponse.ProcessedData, result.Value.ProcessedData);
    await mockContinuation.Received(1).Invoke();
  }

  #endregion

  #region Integration Tests

  [Fact]
  public async Task HandleAsync_WithRealValidator_ShouldPerformActualValidation() {
    // Arrange
    var services = new ServiceCollection();
    services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
    var serviceProvider = services.BuildServiceProvider();

    var decorator = new ValidationDecorator<TestRequest, TestResponse>(serviceProvider, _MockLogger);
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    // Valid request
    var validRequest = new TestRequest("John Doe", "john@example.com");
    var expectedResponse = new TestResponse("processed");
    mockContinuation.Invoke().Returns(Result.Success(expectedResponse));

    // Act
    var result = await decorator.HandleAsync(validRequest, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    await mockContinuation.Received(1).Invoke();
  }

  [Fact]
  public async Task HandleAsync_WithRealValidatorAndInvalidRequest_ShouldFailValidation() {
    // Arrange
    var services = new ServiceCollection();
    services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
    var serviceProvider = services.BuildServiceProvider();

    var decorator = new ValidationDecorator<TestRequest, TestResponse>(serviceProvider, _MockLogger);
    var mockContinuation = Substitute.For<RequestHandlerFunction<TestResponse>>();

    // Invalid request (empty name)
    var invalidRequest = new TestRequest("", "john@example.com");

    // Act
    var result = await decorator.HandleAsync(invalidRequest, mockContinuation, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.Validation, result.Error.Type);
    await mockContinuation.DidNotReceive().Invoke();
  }

  #endregion

  #region Test Validator Implementation

  /// <summary>
  /// Simple test validator for integration testing.
  /// </summary>
  private sealed class TestRequestValidator: IValidator<TestRequest> {
    public IValidationResult Validate(TestRequest instance, IValidationContext? context = null) =>
      ValidateAsync(instance, context, CancellationToken.None).GetAwaiter().GetResult();

    public Task<IValidationResult> ValidateAsync(
      TestRequest instance,
      IValidationContext? context = null,
      CancellationToken cancellationToken = default) {

      var errors = new List<IValidationError>();

      if (string.IsNullOrWhiteSpace(instance.Name)) {
        errors.Add(new ValidationError("REQUIRED", "Name is required.", "Name"));
      }

      if (string.IsNullOrWhiteSpace(instance.Email)) {
        errors.Add(new ValidationError("REQUIRED", "Email is required.", "Email"));
      } else if (!instance.Email.Contains('@', StringComparison.OrdinalIgnoreCase)) {
        errors.Add(new ValidationError("INVALID_EMAIL", "Email format is invalid.", "Email"));
      }

      return Task.FromResult<IValidationResult>(new ValidationResult(errors));
    }
  }

  #endregion
}
