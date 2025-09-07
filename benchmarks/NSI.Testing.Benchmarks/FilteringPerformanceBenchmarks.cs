using System.Diagnostics.CodeAnalysis;

namespace NSI.Testing.Benchmarks;
/// <summary>
/// Benchmarks for filtering performance in MockLoggerFactory.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the performance impact of various filtering configurations
/// and compare filtered vs unfiltered logging performance.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class FilteringPerformanceBenchmarks: IDisposable {
  private ILogEntryStore _Store = null!;
  private ILoggerFactory _UnfilteredFactory = null!;
  private ILoggerFactory _FilteredFactory = null!;
  private ILoggerFactory _HeavyFilteredFactory = null!;
  private ILogger _UnfilteredLogger = null!;
  private ILogger _FilteredLogger = null!;
  private ILogger _HeavyFilteredLogger = null!;

  /// <summary>
  /// Number of log operations to perform.
  /// </summary>
  [Params(1000, 10000)]
  public int OperationCount { get; set; }

  /// <summary>
  /// Setup method called before each benchmark run.
  /// </summary>
  [GlobalSetup]
  public void GlobalSetup() {
    _Store = new InMemoryLogEntryStore();

    // Unfiltered factory - captures everything
    _UnfilteredFactory = new MockLoggerFactory(_Store, new MockLoggerOptions {
      MinimumLevel = LogLevel.Trace,
      CaptureScopes = true
    });

    // Moderately filtered factory - filters debug/trace
    var filteredOptions = new MockLoggerOptions {
      MinimumLevel = LogLevel.Information,
      CaptureScopes = true,
    };
    filteredOptions.CategoryLevels.Add("Microsoft", LogLevel.Warning);
    filteredOptions.CategoryLevels.Add("System", LogLevel.Error);
    _FilteredFactory = new MockLoggerFactory(_Store, filteredOptions);

    // Heavily filtered factory - only errors and warnings
    var heavyOptions = new MockLoggerOptions {
      MinimumLevel = LogLevel.Warning,
      CaptureScopes = false, // No scopes captured
    };
    heavyOptions.CategoryLevels.Add("Microsoft", LogLevel.Critical);
    heavyOptions.CategoryLevels.Add("System", LogLevel.Critical);
    heavyOptions.CategoryLevels.Add("ThirdParty", LogLevel.Critical);
    _HeavyFilteredFactory = new MockLoggerFactory(_Store, heavyOptions);

    _UnfilteredLogger = _UnfilteredFactory.CreateLogger("TestCategory");
    _FilteredLogger = _FilteredFactory.CreateLogger("TestCategory");
    _HeavyFilteredLogger = _HeavyFilteredFactory.CreateLogger("TestCategory");
  }

  /// <summary>
  /// Setup method called before each benchmark iteration.
  /// </summary>
  [IterationSetup]
  public void IterationSetup() => _Store.Clear();

  /// <summary>
  /// Benchmark for unfiltered logging performance.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark(Baseline = true)]
  public int Unfiltered_AllLevels() {
    for (var i = 0; i < OperationCount; i++) {
      using var scope = _UnfilteredLogger.BeginScope($"Operation {i}");

      _UnfilteredLogger.LogTrace("Trace message {Index}", i);
      _UnfilteredLogger.LogDebug("Debug message {Index}", i);
      _UnfilteredLogger.LogInformation("Information message {Index}", i);
      _UnfilteredLogger.LogWarning("Warning message {Index}", i);

      if (i % 100 == 0) {
        _UnfilteredLogger.LogError("Error message {Index}", i);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for moderately filtered logging performance.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int Filtered_InformationAndAbove() {
    for (var i = 0; i < OperationCount; i++) {
      using var scope = _FilteredLogger.BeginScope($"Operation {i}");

      _FilteredLogger.LogTrace("Trace message {Index}", i); // Filtered out
      _FilteredLogger.LogDebug("Debug message {Index}", i); // Filtered out
      _FilteredLogger.LogInformation("Information message {Index}", i);
      _FilteredLogger.LogWarning("Warning message {Index}", i);

      if (i % 100 == 0) {
        _FilteredLogger.LogError("Error message {Index}", i);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for heavily filtered logging performance.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int HeavyFiltered_WarningsAndErrors() {
    for (var i = 0; i < OperationCount; i++) {
      using var scope = _HeavyFilteredLogger.BeginScope($"Operation {i}"); // Scopes filtered out

      _HeavyFilteredLogger.LogTrace("Trace message {Index}", i); // Filtered out
      _HeavyFilteredLogger.LogDebug("Debug message {Index}", i); // Filtered out
      _HeavyFilteredLogger.LogInformation("Information message {Index}", i); // Filtered out
      _HeavyFilteredLogger.LogWarning("Warning message {Index}", i);

      if (i % 100 == 0) {
        _HeavyFilteredLogger.LogError("Error message {Index}", i);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for category-specific filtering.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int CategoryFiltering_MixedCategories() {
    var appLogger = _FilteredFactory.CreateLogger("MyApp.Service");
    var microsoftLogger = _FilteredFactory.CreateLogger("Microsoft.AspNetCore");
    var systemLogger = _FilteredFactory.CreateLogger("System.Net.Http");

    for (var i = 0; i < OperationCount; i++) {
      // App logger - captures Info and above
      appLogger.LogDebug("App debug {Index}", i); // Filtered out
      appLogger.LogInformation("App info {Index}", i);

      // Microsoft logger - only warnings and above
      microsoftLogger.LogDebug("Microsoft debug {Index}", i); // Filtered out
      microsoftLogger.LogInformation("Microsoft info {Index}", i); // Filtered out
      microsoftLogger.LogWarning("Microsoft warning {Index}", i);

      // System logger - only errors and above
      systemLogger.LogWarning("System warning {Index}", i); // Filtered out
      if (i % 200 == 0) {
        systemLogger.LogError("System error {Index}", i);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for IsEnabled check performance impact.
  /// </summary>
  /// <returns>Number of enabled checks performed for verification.</returns>
  [Benchmark]
  public int IsEnabledChecks_PerformanceImpact() {
    var enabledCount = 0;

    for (var i = 0; i < OperationCount; i++) {
      // Simulate checking before logging (good practice)
      if (_FilteredLogger.IsEnabled(LogLevel.Trace)) {
        _FilteredLogger.LogTrace("Trace message {Index}", i);
        enabledCount++;
      }

      if (_FilteredLogger.IsEnabled(LogLevel.Debug)) {
        _FilteredLogger.LogDebug("Debug message {Index}", i);
        enabledCount++;
      }

      if (_FilteredLogger.IsEnabled(LogLevel.Information)) {
        _FilteredLogger.LogInformation("Information message {Index}", i);
        enabledCount++;
      }

      if (_FilteredLogger.IsEnabled(LogLevel.Warning)) {
        _FilteredLogger.LogWarning("Warning message {Index}", i);
        enabledCount++;
      }
    }

    return enabledCount;
  }

  /// <summary>
  /// Benchmark for scope filtering performance.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int ScopeFiltering_Performance() {
    var scopeCaptureLogger = _FilteredFactory.CreateLogger("WithScopes");
    var noScopeLogger = _HeavyFilteredFactory.CreateLogger("NoScopes");

    for (var i = 0; i < OperationCount; i++) {
      // Logger that captures scopes
      using (var scope1 = scopeCaptureLogger.BeginScope($"Scope {i}")) {
        scopeCaptureLogger.LogInformation("Message with scope {Index}", i);
      }

      // Logger that ignores scopes
      using var scope2 = noScopeLogger.BeginScope($"Ignored scope {i}");
      noScopeLogger.LogWarning("Message without scope capture {Index}", i);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for filtering with complex state objects.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int ComplexStateFiltering_Performance() {
    for (var i = 0; i < OperationCount; i++) {
      var complexState = new {
        RequestId = Guid.NewGuid(),
        UserId = i % 1000,
        Timestamp = DateTimeOffset.UtcNow,
        Metadata = new Dictionary<string, object> {
          ["Version"] = "1.0.0",
          ["Environment"] = "Benchmark",
          ["BatchSize"] = OperationCount
        },
        Items = Enumerable.Range(0, 5).Select(x => $"Item_{x}").ToArray()
      };

      // These will be filtered based on level
      _FilteredLogger.LogTrace("Complex trace {Index} for state {@ComplexState}", i, complexState); // Filtered out
      _FilteredLogger.LogDebug("Complex debug {Index} for state {@ComplexState}", i, complexState); // Filtered out
      _FilteredLogger.LogInformation("Complex info {Index} for state {@ComplexState}", i, complexState);

      if (i % 50 == 0) {
        _FilteredLogger.LogWarning("Complex warning {Index} for state {@ComplexState}", i, complexState);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for concurrent filtering performance.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int ConcurrentFiltering_ThreadSafety() {
    var tasks = new Task[Environment.ProcessorCount];

    for (var t = 0; t < tasks.Length; t++) {
      var threadId = t;
      tasks[t] = Task.Run(() => {
        var logger = _FilteredFactory.CreateLogger($"Thread{threadId}");
        var operationsPerThread = OperationCount / Environment.ProcessorCount;

        for (var i = 0; i < operationsPerThread; i++) {
          using var scope = logger.BeginScope($"Thread {threadId} Operation {i}");

          logger.LogDebug("Debug from thread {ThreadId}", threadId); // Filtered out
          logger.LogInformation("Info from thread {ThreadId} operation {Index}", threadId, i);

          if (i % 20 == 0) {
            logger.LogWarning("Warning from thread {ThreadId}", threadId);
          }
        }
      });
    }

    Task.WaitAll(tasks);

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for real-world application scenario with filtering.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int RealWorldFiltering_ApplicationScenario() {
    // Simulate various application components
    var controllers = new[] {
      _FilteredFactory.CreateLogger("MyApp.Controllers.UserController"),
      _FilteredFactory.CreateLogger("MyApp.Controllers.OrderController")
    };

    var services = new[] {
      _FilteredFactory.CreateLogger("MyApp.Services.UserService"),
      _FilteredFactory.CreateLogger("MyApp.Services.OrderService"),
      _FilteredFactory.CreateLogger("MyApp.Services.PaymentService")
    };

    var infrastructure = new[] {
      _FilteredFactory.CreateLogger("Microsoft.EntityFrameworkCore"), // Will be filtered
      _FilteredFactory.CreateLogger("Microsoft.AspNetCore.Mvc"), // Will be filtered
      _FilteredFactory.CreateLogger("System.Net.Http.HttpClient") // Will be filtered
    };

    for (var i = 0; i < OperationCount; i++) {
      var requestId = Guid.NewGuid();

      // Controller logging
      var controller = controllers[i % controllers.Length];
      using var controllerScope = controller.BeginScope($"Request {requestId}");
      controller.LogInformation("Request received {RequestId}", requestId);

      // Service logging
      var service = services[i % services.Length];
      using (var serviceScope = service.BeginScope($"Processing {requestId}")) {
        service.LogDebug("Service processing started"); // May be filtered
        service.LogInformation("Service operation completed");

        if (i % 100 == 0) {
          service.LogWarning("Service warning occurred");
        }
      }

      // Infrastructure logging (mostly filtered)
      var infra = infrastructure[i % infrastructure.Length];
      infra.LogDebug("Infrastructure debug"); // Filtered out
      infra.LogInformation("Infrastructure info"); // Filtered out
      infra.LogWarning("Infrastructure warning"); // May be captured depending on category

      controller.LogInformation("Request completed {RequestId}", requestId);
    }

    return _Store.GetAll().Count;
  }

  public void Dispose() {
    _UnfilteredFactory?.Dispose();
    _FilteredFactory?.Dispose();
    _HeavyFilteredFactory?.Dispose();
    _Store?.Clear();
  }
}
