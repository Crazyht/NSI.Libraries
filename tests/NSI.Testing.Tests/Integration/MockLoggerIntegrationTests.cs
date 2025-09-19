using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSI.Testing.Loggers;

namespace NSI.Testing.Tests.Integration;
/// <summary>
/// Integration tests for the MockLogger system with dependency injection and real-world
/// scenarios.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the complete MockLogger system integration including
/// dependency injection setup, multi-logger scenarios, and complex logging workflows.
/// </para>
/// </remarks>
public class MockLoggerIntegrationTests {

  /// <summary>
  /// Test service that uses ILogger for integration testing.
  /// </summary>
  private sealed class TestService(ILogger<MockLoggerIntegrationTests.TestService> logger) {
    private readonly ILogger<TestService> _Logger =
      logger ?? throw new ArgumentNullException(nameof(logger));

    public void ProcessOrder(int orderId, string customerName) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "OrderId", orderId },
        { "CustomerName", customerName },
        { "Operation", "ProcessOrder" }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(100),
        "Order processing started",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      ValidateOrder(orderId);
      ProcessPayment(orderId);
      ShipOrder(orderId);

      _Logger.Log(
        LogLevel.Information,
        new EventId(199),
        "Order processing completed",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    private void ValidateOrder(int orderId) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Step", "Validation" },
        { "OrderId", orderId }
      });

      _Logger.Log(
        LogLevel.Debug,
        new EventId(101),
        "Validating order details",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      if (orderId <= 0) {
        _Logger.Log(
          LogLevel.Error,
          new EventId(102),
          "Invalid order ID",
          new ArgumentException("Order ID must be positive"),
          (s, ex) => $"{s}: {ex?.Message}");
        throw new ArgumentException("Order ID must be positive");
      }

      _Logger.Log(
        LogLevel.Information,
        new EventId(103),
        "Order validation completed",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    private void ProcessPayment(int orderId) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Step", "Payment" },
        { "OrderId", orderId }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(110),
        "Processing payment",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      // Simulate payment processing
      Thread.Sleep(5); // Small delay to simulate work

      if (orderId == 999) {
        // Simulate payment failure
        _Logger.Log(
          LogLevel.Error,
          new EventId(111),
          "Payment processing failed",
          new InvalidOperationException("Payment gateway timeout"),
          (s, ex) => $"{s}: {ex?.Message}");
        throw new InvalidOperationException("Payment gateway timeout");
      }

      _Logger.Log(
        LogLevel.Information,
        new EventId(119),
        "Payment processed successfully",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    private void ShipOrder(int orderId) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Step", "Shipping" },
        { "OrderId", orderId }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(120),
        "Preparing order for shipping",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      _Logger.Log(
        LogLevel.Information,
        new EventId(121),
        "Order shipped",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    public async Task ProcessOrderAsync(int orderId, string customerName) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "OrderId", orderId },
        { "CustomerName", customerName },
        { "Operation", "ProcessOrderAsync" }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(200),
        "Async order processing started",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      await ValidateOrderAsync(orderId);
      await ProcessPaymentAsync(orderId);
      await ShipOrderAsync(orderId);

      _Logger.Log(
        LogLevel.Information,
        new EventId(299),
        "Async order processing completed",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    private async Task ValidateOrderAsync(int orderId) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Step", "AsyncValidation" },
        { "OrderId", orderId }
      });

      _Logger.Log(
        LogLevel.Debug,
        new EventId(201),
        "Starting Async validation",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      await Task.Delay(10); // Simulate async work

      _Logger.Log(
        LogLevel.Information,
        new EventId(202),
        "Async validation completed",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    private async Task ProcessPaymentAsync(int orderId) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Step", "AsyncPayment" },
        { "OrderId", orderId }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(210),
        "Starting async payment",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      await Task.Delay(15); // Simulate async payment processing

      _Logger.Log(
        LogLevel.Information,
        new EventId(211),
        "Async payment completed",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    private async Task ShipOrderAsync(int orderId) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Step", "AsyncShipping" },
        { "OrderId", orderId }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(220),
        "Starting async shipping",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      await Task.Delay(5); // Simulate async shipping

      _Logger.Log(
        LogLevel.Information,
        new EventId(221),
        "Async shipping completed",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }
  }

  /// <summary>
  /// Another test service for multi-logger scenarios.
  /// </summary>
  private sealed class NotificationService(ILogger<NotificationService> logger) {
    private readonly ILogger<NotificationService> _Logger =
      logger ?? throw new ArgumentNullException(nameof(logger));

    public void SendNotification(string recipient) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Recipient", recipient },
        { "NotificationType", "Email" }
      });

      _Logger.Log(
        LogLevel.Information,
        new EventId(300),
        "Sending notification",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      // Simulate notification sending
      if (string.IsNullOrEmpty(recipient)) {
        _Logger.Log(
          LogLevel.Error,
          new EventId(301),
          "Invalid recipient",
          new ArgumentException("Recipient cannot be empty"),
          (s, ex) => $"{s}: {ex?.Message}");
        return;
      }

      _Logger.Log(
        LogLevel.Information,
        new EventId(302),
        "Notification sent successfully",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }
  }

  [Fact]
  public void DependencyInjection_WithMockLogger_ShouldWorkCorrectly() {
    // Setup dependency injection
    var services = new ServiceCollection();
    var store = new InMemoryLogEntryStore();

    services.AddSingleton<ILogEntryStore>(store);
    services.AddSingleton(typeof(ILogger<>), typeof(MockLogger<>));
    services.AddTransient<TestService>();

    var serviceProvider = services.BuildServiceProvider();

    // Execute service operation
    var testService = serviceProvider.GetRequiredService<TestService>();
    testService.ProcessOrder(12345, "John Doe");

    // Verify logging through DI
    var entries = store.GetAll();
    Assert.True(entries.Count > 0);

    var logEntries = entries.LogsOnly().ToList();
    Assert.Contains(
      logEntries,
      e => e.Message?.Contains("Order processing started", StringComparison.Ordinal) == true);
    Assert.Contains(
      logEntries,
      e => e.Message?.Contains("Order processing completed", StringComparison.Ordinal) == true);
  }

  [Fact]
  public void MultipleLoggers_WithSharedStore_ShouldCaptureAllEntries() {
    // Setup multiple services with shared store
    var store = new InMemoryLogEntryStore();
    var testService = new TestService(new MockLogger<TestService>(store));
    var notificationService = new NotificationService(new MockLogger<NotificationService>(store));

    // Execute operations on both services
    testService.ProcessOrder(12345, "John Doe");
    notificationService.SendNotification("john@example.com");

    // Verify all entries are captured
    var entries = store.GetAll();
    var logEntries = entries.LogsOnly().ToList();

    // Should have entries from both services
    Assert.Contains(
      logEntries,
      e => e.Message?.Contains("Order processing", StringComparison.Ordinal) == true);
    Assert.Contains(
      logEntries,
      e => e.Message?.Contains("Notification sent", StringComparison.Ordinal) == true);

    // Verify scope separation
    var scopes = entries.ScopeStartsOnly().ToList();
    Assert.True(scopes.Count >= 2); // At least one scope from each service
  }

  [Fact]
  public void ComplexWorkflow_WithNestedScopes_ShouldMaintainHierarchy() {
    // Setup
    var store = new InMemoryLogEntryStore();
    var service = new TestService(new MockLogger<TestService>(store));

    // Execute complex workflow
    service.ProcessOrder(12345, "John Doe");

    // Analyze the complete workflow
    var entries = store.GetAll();

    // Verify main scope
    var mainScope = entries
      .ScopeStartsOnly()
      .WithScopeContainingVar("Operation", "ProcessOrder")
      .First();

    // Verify nested scopes
    var nestedScopes = entries
      .ScopeStartsOnly()
      .WithParentScope(mainScope.ScopeId!.Value)
      .ToList();

    Assert.Equal(3, nestedScopes.Count); // Validation, Payment, Shipping

    // Verify step scopes
    var validationScope =
      nestedScopes.WithScopeContainingVar("Step", "Validation").First();
    var paymentScope = nestedScopes.WithScopeContainingVar("Step", "Payment").First();
    var shippingScope = nestedScopes.WithScopeContainingVar("Step", "Shipping").First();

    Assert.NotEqual(validationScope.ScopeId, paymentScope.ScopeId);
    Assert.NotEqual(paymentScope.ScopeId, shippingScope.ScopeId);

    // Verify logs are properly scoped
    var validationLogs = entries
      .WithinScope(validationScope.ScopeId!.Value)
      .LogsOnly()
      .ToList();
    var paymentLogs = entries
      .WithinScope(paymentScope.ScopeId!.Value)
      .LogsOnly()
      .ToList();
    var shippingLogs = entries
      .WithinScope(shippingScope.ScopeId!.Value)
      .LogsOnly()
      .ToList();

    Assert.True(validationLogs.Count >= 2);
    Assert.True(paymentLogs.Count >= 2);
    Assert.True(shippingLogs.Count >= 2);
  }

  [Fact]
  public void ErrorHandling_WithExceptions_ShouldCaptureErrorDetails() {
    // Setup
    var store = new InMemoryLogEntryStore();
    var service = new TestService(new MockLogger<TestService>(store));

    // Execute operation that should fail
    Assert.Throws<ArgumentException>(() => service.ProcessOrder(-1, "Invalid Order"));

    // Analyze error logging
    var entries = store.GetAll();
    var errorEntries = entries
      .LogsOnly()
      .WithLogLevel(LogLevel.Error)
      .ToList();

    Assert.Single(errorEntries);

    var errorEntry = errorEntries[0];
    Assert.NotNull(errorEntry.Exception);
    Assert.IsType<ArgumentException>(errorEntry.Exception);
    Assert.Contains(
      "Invalid order ID",
      errorEntry.Message ?? string.Empty,
      StringComparison.Ordinal);

    // Verify error occurred in validation scope
    Assert.NotNull(errorEntry.ScopeId);
    var errorScope = entries
      .ScopeStartsOnly()
      .WithinScope(errorEntry.ScopeId.Value)
      .WithScopeContainingVar("Step", "Validation")
      .FirstOrDefault();

    Assert.NotNull(errorScope);
  }

  [Fact]
  public async Task AsyncOperations_WithScopeContext_ShouldPreserveHierarchy() {
    // Setup
    var store = new InMemoryLogEntryStore();
    var service = new TestService(new MockLogger<TestService>(store));

    // Execute async operation
    await service.ProcessOrderAsync(12345, "Jane Doe");

    // Verify async scope preservation
    var entries = store.GetAll();

    // Verify main async scope
    var mainScope = entries
      .ScopeStartsOnly()
      .WithScopeContainingVar("Operation", "ProcessOrderAsync")
      .First();

    // Verify nested async scopes
    var asyncScopes = entries
      .ScopeStartsOnly()
      .WithParentScope(mainScope.ScopeId!.Value)
      .ToList();

    Assert.Equal(3, asyncScopes.Count); // AsyncValidation, AsyncPayment, AsyncShipping

    // Verify async logs are properly scoped
    var asyncLogs = entries
      .LogsOnly()
      .Where(e => e.Message?.Contains("Async", StringComparison.Ordinal) == true)
      .ToList();

    Assert.True(asyncLogs.Count >= 6); // At least 2 logs per async step
    Assert.All(asyncLogs, log => Assert.NotNull(log.ScopeId));

    // Verify scope hierarchy is maintained across async calls
    var validationScope =
      asyncScopes.WithScopeContainingVar("Step", "AsyncValidation").First();
    var validationLogs = entries
      .WithinScope(validationScope.ScopeId!.Value)
      .LogsOnly()
      .ToList();

    Assert.True(validationLogs.Count >= 2);
    Assert.All(validationLogs, log => Assert.Equal(validationScope.ScopeId, log.ScopeId));
  }

  [Fact]
  public async Task ConcurrentOperations_WithSeparateScopes_ShouldMaintainIsolation() {
    // Setup
    var store = new InMemoryLogEntryStore();
    var service = new TestService(new MockLogger<TestService>(store));
    const int concurrentOperations = 5;

    var tasks = new Task[concurrentOperations];

    // Execute concurrent operations
    for (var i = 0; i < concurrentOperations; i++) {
      var orderId = 1000 + i;
      var customerName = $"Customer {i}";

      tasks[i] = Task.Run(() => service.ProcessOrder(orderId, customerName));
    }

    await Task.WhenAll(tasks);

    // Verify concurrent scope isolation
    var entries = store.GetAll();
    var mainScopes = entries
      .ScopeStartsOnly()
      .WithScopeContainingVar("Operation", "ProcessOrder")
      .ToList();

    Assert.Equal(concurrentOperations, mainScopes.Count);

    // Verify each operation has unique scope
    var scopeIds = mainScopes.Select(s => s.ScopeId!.Value).ToList();
    Assert.Equal(concurrentOperations, scopeIds.Distinct().Count());

    // Verify each scope has complete workflow
    foreach (var scope in mainScopes) {
      var scopeLogs = entries
        .WithinScope(scope.ScopeId!.Value)
        .LogsOnly()
        .ToList();
      Assert.Contains(
        scopeLogs,
        log => log.Message?.Contains("Order processing started", StringComparison.Ordinal) == true);
      Assert.Contains(
        scopeLogs,
        log => log.Message?.Contains("Order processing completed", StringComparison.Ordinal) == true);
    }
  }

  [Fact]
  public void RealWorldScenario_OrderProcessingWorkflow_ShouldProvideComprehensiveLogging() {
    // Setup realistic scenario
    var store = new InMemoryLogEntryStore();
    var orderService = new TestService(new MockLogger<TestService>(store));
    var notificationService = new NotificationService(new MockLogger<NotificationService>(store));

    // Execute complete order workflow
    orderService.ProcessOrder(12345, "John Doe");
    notificationService.SendNotification("john@example.com");

    // Comprehensive analysis using LINQ extensions
    var analysisResult = AnalyzeOrderWorkflow(store);

    // Verify comprehensive workflow analysis
    Assert.True(analysisResult.TotalLogEntries > 0);
    Assert.True(analysisResult.TotalScopes > 0);
    Assert.True(analysisResult.ProcessingSteps.Count >= 3);
    Assert.True(analysisResult.NotificationsSent > 0);
    Assert.Equal(0, analysisResult.ErrorsEncountered);

    // Verify workflow timing analysis
    Assert.True(analysisResult.WorkflowDuration.HasValue);
    Assert.Contains(analysisResult.ProcessingSteps, step => step.StepName == "Validation");
    Assert.Contains(analysisResult.ProcessingSteps, step => step.StepName == "Payment");
    Assert.Contains(analysisResult.ProcessingSteps, step => step.StepName == "Shipping");
  }

  [Fact]
  public void ErrorScenario_PaymentFailure_ShouldProvideDetailedErrorAnalysis() {
    // Setup error scenario
    var store = new InMemoryLogEntryStore();
    var orderService = new TestService(new MockLogger<TestService>(store));

    // Execute operation that will fail at payment
    Assert.Throws<InvalidOperationException>(() =>
      orderService.ProcessOrder(999, "Failed Payment Customer"));

    // Analyze error scenario
    var errorAnalysis = AnalyzeOrderErrors(store);

    // Verify error analysis
    Assert.True(errorAnalysis.TotalErrors > 0);
    Assert.Contains(errorAnalysis.ErrorsByStep, kvp => kvp.Key == "Payment");
    Assert.Contains("InvalidOperationException", errorAnalysis.ExceptionTypes);
    Assert.Contains(
      errorAnalysis.ErrorMessages,
      msg => msg.Contains("Payment processing failed", StringComparison.Ordinal));

    // Verify partial workflow completion
    Assert.Contains("Validation", errorAnalysis.CompletedSteps);
    Assert.DoesNotContain("Shipping", errorAnalysis.CompletedSteps);
  }

  [Fact]
  public async Task PerformanceScenario_HighVolumeLogging_ShouldMaintainPerformance() {
    // Setup high-volume scenario
    var store = new InMemoryLogEntryStore();
    var service = new TestService(new MockLogger<TestService>(store));
    const int operationCount = 100;

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // Execute high-volume operations
    var tasks = new Task[operationCount];
    for (var i = 0; i < operationCount; i++) {
      var orderId = 10000 + i;
      tasks[i] = Task.Run(() => service.ProcessOrder(orderId, $"Customer {orderId}"));
    }

    await Task.WhenAll(tasks);
    stopwatch.Stop();

    // Verify performance characteristics
    var entries = store.GetAll();
    Assert.True(entries.Count > operationCount * 5);

    // Performance should be reasonable
    Assert.True(
      stopwatch.ElapsedMilliseconds < 5000,
      $"High-volume logging took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

    // Verify LINQ query performance on large dataset
    var queryStopwatch = System.Diagnostics.Stopwatch.StartNew();

    var performanceAnalysis = entries
      .LogsOnly()
      .WithLogLevel(LogLevel.Information)
      .GroupBy(e => e.Message?.Split(' ').FirstOrDefault())
      .Where(g => g.Count() > 10)
      .Select(g => new { MessageStart = g.Key, Count = g.Count() })
      .OrderByDescending(x => x.Count)
      .ToList();

    queryStopwatch.Stop();

    Assert.True(
      queryStopwatch.ElapsedMilliseconds < 100,
      $"LINQ query on large dataset took {queryStopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    Assert.True(performanceAnalysis.Count > 0);
  }

  [Fact]
  public void DependencyInjection_WithMultipleServices_ShouldShareStore() {
    // Setup comprehensive DI scenario
    var services = new ServiceCollection();
    var store = new InMemoryLogEntryStore();

    services.AddSingleton<ILogEntryStore>(store);
    services.AddSingleton(typeof(ILogger<>), typeof(MockLogger<>));
    services.AddTransient<TestService>();
    services.AddTransient<NotificationService>();

    var serviceProvider = services.BuildServiceProvider();

    // Execute operations through DI
    var orderService = serviceProvider.GetRequiredService<TestService>();
    var notificationService = serviceProvider.GetRequiredService<NotificationService>();

    orderService.ProcessOrder(12345, "DI Customer");
    notificationService.SendNotification("di@example.com");

    // Verify shared store captures all operations
    var entries = store.GetAll();
    var logEntries = entries.LogsOnly().ToList();

    // Should have logs from both services
    var orderLogs = logEntries
      .Where(e => e.Message?.Contains("Order", StringComparison.Ordinal) == true)
      .ToList();
    var notificationLogs = logEntries
      .Where(e => e.Message?.Contains("Notification", StringComparison.Ordinal) == true)
      .ToList();

    Assert.True(orderLogs.Count > 0);
    Assert.True(notificationLogs.Count > 0);

    // Verify scopes are properly isolated despite shared store
    var orderScopes = entries
      .ScopeStartsOnly()
      .WithScopeContainingVar("Operation", "ProcessOrder")
      .ToList();
    var notificationScopes = entries
      .ScopeStartsOnly()
      .WithScopeContainingKey("Recipient")
      .ToList();

    Assert.True(orderScopes.Count > 0);
    Assert.True(notificationScopes.Count > 0);
    Assert.NotEqual(orderScopes[0].ScopeId, notificationScopes[0].ScopeId);
  }

  /// <summary>
  /// Analyzes order workflow logs to provide comprehensive insights.
  /// </summary>
  private static OrderWorkflowAnalysis AnalyzeOrderWorkflow(InMemoryLogEntryStore store) {
    ArgumentNullException.ThrowIfNull(store);

    var entries = store.GetAll();

    var analysis = new OrderWorkflowAnalysis {
      TotalLogEntries = entries.LogsOnly().Count(),
      TotalScopes = entries.ScopeStartsOnly().Count(),
      ProcessingSteps = [.. entries
        .ScopeStartsOnly()
        .WithScopeContainingKey("Step")
        .Select(scope => new ProcessingStep {
          StepName = scope.State
            .OfType<KeyValuePair<string, object>>()
            .First(kv => kv.Key == "Step")
            .Value
            .ToString() ?? "Unknown",
          LogCount = entries
            .WithinScope(scope.ScopeId!.Value)
            .LogsOnly()
            .Count()
        })],
      NotificationsSent = entries
        .LogsOnly()
        .Count(e =>
          e.Message?.Contains("Notification sent", StringComparison.Ordinal) == true),
      ErrorsEncountered = entries.LogsOnly().WithException().Count()
    };

    // Calculate workflow duration based on event IDs (simple approximation)
    var firstEvent = entries.LogsOnly().MinBy(e => e.EventId?.Id ?? 0);
    var lastEvent = entries.LogsOnly().MaxBy(e => e.EventId?.Id ?? 0);

    if (firstEvent != null && lastEvent != null &&
        firstEvent.EventId.HasValue && lastEvent.EventId.HasValue) {
      analysis.WorkflowDuration = TimeSpan.FromMilliseconds(
        (lastEvent.EventId.Value.Id - firstEvent.EventId.Value.Id) * 10);
    }

    return analysis;
  }

  /// <summary>
  /// Analyzes order processing errors to provide detailed error insights.
  /// </summary>
  private static OrderErrorAnalysis AnalyzeOrderErrors(InMemoryLogEntryStore store) {
    ArgumentNullException.ThrowIfNull(store);

    var entries = store.GetAll();
    var errorEntries = entries.LogsOnly().WithException().ToList();

    return new OrderErrorAnalysis {
      TotalErrors = errorEntries.Count,
      ErrorsByStep = errorEntries
        .Where(e => e.ScopeId.HasValue)
        .GroupBy(e => entries
          .ScopeStartsOnly()
          .WithinScope(e.ScopeId!.Value)
          .SelectMany(s => s.State.OfType<KeyValuePair<string, object>>())
          .FirstOrDefault(kv => kv.Key == "Step")
          .Value?
          .ToString() ?? "Unknown")
        .ToDictionary(g => g.Key, g => g.Count()),
      ExceptionTypes = [.. errorEntries
        .Select(e => e.Exception?.GetType().Name ?? "Unknown")
        .Distinct()],
      ErrorMessages = [.. errorEntries.Select(e => e.Message ?? "No message")],
      CompletedSteps = [.. entries
        .LogsOnly()
        .Where(e => e.Message?.Contains("completed", StringComparison.Ordinal) == true)
        .Where(e => e.ScopeId.HasValue)
        .Select(e => entries
          .ScopeStartsOnly()
          .WithinScope(e.ScopeId!.Value)
          .SelectMany(s => s.State.OfType<KeyValuePair<string, object>>())
          .FirstOrDefault(kv => kv.Key == "Step")
          .Value?
          .ToString() ?? "Unknown")
        .Distinct()]
    };
  }

  /// <summary>
  /// Analysis result for order workflow.
  /// </summary>
  private sealed class OrderWorkflowAnalysis {
    public int TotalLogEntries { get; set; }
    public int TotalScopes { get; set; }
    public List<ProcessingStep> ProcessingSteps { get; set; } = [];
    public int NotificationsSent { get; set; }
    public int ErrorsEncountered { get; set; }
    public TimeSpan? WorkflowDuration { get; set; }
  }

  /// <summary>
  /// Represents a processing step in the workflow.
  /// </summary>
  private sealed class ProcessingStep {
    public required string StepName { get; set; }
    public int LogCount { get; set; }
  }

  /// <summary>
  /// Analysis result for order processing errors.
  /// </summary>
  private sealed class OrderErrorAnalysis {
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsByStep { get; set; } = [];
    public List<string> ExceptionTypes { get; set; } = [];
    public List<string> ErrorMessages { get; set; } = [];
    public List<string> CompletedSteps { get; set; } = [];
  }
}
