using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSI.Core.Mediator;
using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NSI.Core.Tests.Mediator;
/// <summary>
/// Tests for the core Mediator implementation.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the basic functionality of the mediator including
/// request processing, notification dispatching, and error handling.
/// </para>
/// </remarks>
public class MediatorTests {
  private readonly ILogger<MediatorImplementation> _Logger;

  public MediatorTests() => _Logger = NullLogger<MediatorImplementation>.Instance;

  #region Constructor Tests

  [Fact]
  public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException() {
    // Act & Assert
    var ex = Assert.Throws<ArgumentNullException>(() => new MediatorImplementation(null!, _Logger));
    Assert.Equal("serviceProvider", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithNullLogger_ShouldThrowArgumentNullException() {
    // Act & Assert
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_Logger);
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var ex = Assert.Throws<ArgumentNullException>(() => new MediatorImplementation(serviceProvider, null!));
    Assert.Equal("logger", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithValidParameters_ShouldCreateInstance() {
    // Act
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_Logger);
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = new MediatorImplementation(serviceProvider, _Logger);

    // Assert
    Assert.NotNull(mediator);
  }

  #endregion

  #region ProcessAsync Tests

  [Fact]
  public async Task ProcessAsync_WithNullRequest_ShouldThrowArgumentNullException() {
    // Act & Assert
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();
    var ex = await Assert.ThrowsAsync<ArgumentNullException>(
      () => mediator.ProcessAsync<string>(null!));
    Assert.Equal("request", ex.ParamName);
  }

  [Fact]
  public async Task ProcessAsync_WithValidQueryAndHandler_ShouldReturnSuccess() {
    // Arrange
    var query = new TestQuery("test-data");
    var expectedResponse = new TestResponse("processed-data");
    var mockHandler = Substitute.For<IRequestHandler<TestQuery, TestResponse>>();

    mockHandler.HandleAsync(query, Arg.Any<CancellationToken>())
      .Returns(Result.Success(expectedResponse));


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    var result = await mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedResponse.ProcessedData, result.Value.ProcessedData);

    await mockHandler.Received(1).HandleAsync(query, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task ProcessAsync_WithNoHandlerRegistered_ShouldReturnNotFoundError() {
    // Arrange
    var query = new TestQuery("test-data");


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    var result = await mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.NotFound, result.Error.Type);
    Assert.Equal("HANDLER_NOT_FOUND", result.Error.Code);
    Assert.Contains("TestQuery", result.Error.Message, StringComparison.Ordinal);
  }

  [Fact]
  public async Task ProcessAsync_WithHandlerThatFails_ShouldReturnFailureResult() {
    // Arrange
    var query = new TestQuery("test-data");
    var expectedError = ResultError.BusinessRule("BUSINESS_ERROR", "Business logic failed");
    var mockHandler = Substitute.For<IRequestHandler<TestQuery, TestResponse>>();

    mockHandler.HandleAsync(query, Arg.Any<CancellationToken>())
      .Returns(Result.Failure<TestResponse>(expectedError));


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    var result = await mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(expectedError.Type, result.Error.Type);
    Assert.Equal(expectedError.Code, result.Error.Code);
    Assert.Equal(expectedError.Message, result.Error.Message);
  }

  [Fact]
  public async Task ProcessAsync_WithHandlerThatThrowsException_ShouldReturnServiceUnavailableError() {
    // Arrange
    var query = new TestQuery("test-data");
    var expectedException = new InvalidOperationException("Handler failed");
    var mockHandler = Substitute.For<IRequestHandler<TestQuery, TestResponse>>();

    mockHandler.HandleAsync(query, Arg.Any<CancellationToken>())
      .ThrowsAsync(expectedException);


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    var result = await mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.ServiceUnavailable, result.Error.Type);
    Assert.Equal("MEDIATOR_PROCESSING_ERROR", result.Error.Code);
    Assert.Equal(expectedException, result.Error.Exception);
  }

  [Fact]
  public async Task ProcessAsync_WithCancellationToken_ShouldPassTokenToHandler() {
    // Arrange
    var query = new TestQuery("test-data");
    var expectedResponse = new TestResponse("processed-data");
    var cancellationToken = new CancellationToken();
    var mockHandler = Substitute.For<IRequestHandler<TestQuery, TestResponse>>();

    mockHandler.HandleAsync(query, cancellationToken)
      .Returns(Result.Success(expectedResponse));


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    var result = await mediator.ProcessAsync(query, cancellationToken);

    // Assert
    Assert.True(result.IsSuccess);
    await mockHandler.Received(1).HandleAsync(query, cancellationToken);
  }

  [Fact]
  public async Task ProcessAsync_WithCancelledToken_ShouldReturnCancelledError() {
    // Arrange
    var query = new TestQuery("test-data");
    var mockHandler = Substitute.For<IRequestHandler<TestQuery, TestResponse>>();

    mockHandler.HandleAsync(query, Arg.Any<CancellationToken>())
      .Throws(new OperationCanceledException());


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    var result = await mediator.ProcessAsync(query);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal(ErrorType.ServiceUnavailable, result.Error.Type);
    Assert.Equal("REQUEST_CANCELLED", result.Error.Code);
  }

  #endregion

  #region DispatchAsync Tests

  [Fact]
  public async Task DispatchAsync_WithNullNotification_ShouldThrowArgumentNullException() {
    // Act & Assert

    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();
    var ex = await Assert.ThrowsAsync<ArgumentNullException>(
      () => mediator.DispatchAsync<TestNotification>(null!));
    Assert.Equal("notification", ex.ParamName);
  }

  [Fact]
  public async Task DispatchAsync_WithSingleHandler_ShouldExecuteHandler() {
    // Arrange
    var notification = new TestNotification("test-event");
    var mockHandler = Substitute.For<IRequestHandler<TestNotification, Unit>>();

    mockHandler.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));

    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(sp => mockHandler);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();

    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    await mediator.DispatchAsync(notification);

    // Assert
    await mockHandler.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task DispatchAsync_WithMultipleHandlers_ShouldExecuteAllHandlers() {
    // Arrange
    var notification = new TestNotification("test-event");
    var mockHandler1 = Substitute.For<IRequestHandler<TestNotification, Unit>>();
    var mockHandler2 = Substitute.For<IRequestHandler<TestNotification, Unit>>();
    var mockHandler3 = Substitute.For<IRequestHandler<TestNotification, Unit>>();

    mockHandler1.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));

    mockHandler2.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));

    mockHandler3.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler1);
    serviceCollection.AddTransient(_ => mockHandler2);
    serviceCollection.AddTransient(_ => mockHandler3);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    await mediator.DispatchAsync(notification);

    // Assert
    await mockHandler1.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
    await mockHandler2.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
    await mockHandler3.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task DispatchAsync_WithNoHandlers_ShouldCompleteSuccessfully() {
    // Arrange
    var notification = new TestNotification("test-event");

    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act & Assert
    await mediator.DispatchAsync(notification); // Should not throw
    Assert.True(true); // If we reach here, the test passes
  }

  [Fact]
  public async Task DispatchAsync_WithOneHandlerFailing_ShouldNotAffectOtherHandlers() {
    // Arrange
    var notification = new TestNotification("test-event");
    var mockHandler1 = Substitute.For<IRequestHandler<TestNotification, Unit>>();
    var mockHandler2 = Substitute.For<IRequestHandler<TestNotification, Unit>>();
    var mockHandler3 = Substitute.For<IRequestHandler<TestNotification, Unit>>();

    mockHandler1.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));

    mockHandler2.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Throws(new InvalidOperationException("Handler 2 failed"));

    mockHandler3.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler1);
    serviceCollection.AddTransient(_ => mockHandler2);
    serviceCollection.AddTransient(_ => mockHandler3);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();


    // Act
    await mediator.DispatchAsync(notification);

    // Assert
    await mockHandler1.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
    await mockHandler2.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
    await mockHandler3.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task DispatchAsync_WithHandlerReturningFailure_ShouldContinueExecution() {
    // Arrange
    var notification = new TestNotification("test-event");
    var mockHandler1 = Substitute.For<IRequestHandler<TestNotification, Unit>>();
    var mockHandler2 = Substitute.For<IRequestHandler<TestNotification, Unit>>();

    mockHandler1.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Failure<Unit>(ResultError.BusinessRule("BUSINESS_ERROR", "Handler 1 failed")));

    mockHandler2.HandleAsync(notification, Arg.Any<CancellationToken>())
      .Returns(Result.Success(Unit.Value));


    var serviceCollection = new ServiceCollection();
    serviceCollection.AddTransient(_ => mockHandler1);
    serviceCollection.AddTransient(_ => mockHandler2);
    serviceCollection.AddSingleton(_Logger);
    serviceCollection.AddSingleton<MediatorImplementation>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var mediator = serviceProvider.GetRequiredService<MediatorImplementation>();

    // Act
    await mediator.DispatchAsync(notification);

    // Assert
    await mockHandler1.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
    await mockHandler2.Received(1).HandleAsync(notification, Arg.Any<CancellationToken>());
  }

  #endregion
}

#region Test Helper Types

/// <summary>
/// Test query for unit testing.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Public Needed for mock.")]
public record TestQuery(string Data): IQuery<TestResponse>;

/// <summary>
/// Test response for unit testing.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Public Needed for mock.")]
public record TestResponse(string ProcessedData);

/// <summary>
/// Test command for unit testing.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Public Needed for mock.")]
public record TestCommand(string Data): ICommand<TestResponse>;

/// <summary>
/// Test command without response for unit testing.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Public Needed for mock.")]
public record TestCommandWithoutResponse(string Data): ICommand;

/// <summary>
/// Test notification for unit testing.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Public Needed for mock.")]
public record TestNotification(string EventData): INotification;

#endregion
