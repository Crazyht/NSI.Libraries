using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace NSI.Testing.Benchmarks {
  /// <summary>
  /// Comparative benchmarks between MockLogger and standard logging implementations.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These benchmarks compare MockLogger performance against standard .NET logging
  /// implementations to validate that the mock implementation has acceptable overhead
  /// for testing scenarios.
  /// </para>
  /// </remarks>
  [MemoryDiagnoser]
  [ThreadingDiagnoser]
  [SimpleJob]
  [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
  public class ComparisonBenchmarks: IDisposable {
    private ILogEntryStore _MockStore = null!;
    private ILogger<ComparisonBenchmarks> _MockLogger = null!;
    private ILogger<ComparisonBenchmarks> _ConsoleLogger = null!;
    private ILogger<ComparisonBenchmarks> _NullLogger = null!;
    private ILoggerFactory _MockFactory = null!;
    private ILoggerFactory _ConsoleFactory = null!;
    private ServiceProvider _ServiceProvider = null!;

    /// <summary>
    /// Number of operations for comparison testing.
    /// </summary>
    [Params(1000, 10000)]
    public int OperationCount { get; set; }

    /// <summary>
    /// Setup method called before each benchmark run.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup() {
      // Setup MockLogger
      _MockStore = new InMemoryLogEntryStore();
      _MockLogger = new MockLogger<ComparisonBenchmarks>(_MockStore);
      _MockFactory = new MockLoggerFactory(_MockStore);

      // Setup standard console logger
      var services = new ServiceCollection();
      services.AddLogging(builder => {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Trace);
      });
      _ServiceProvider = services.BuildServiceProvider();
      _ConsoleLogger = _ServiceProvider.GetRequiredService<ILogger<ComparisonBenchmarks>>();
      _ConsoleFactory = _ServiceProvider.GetRequiredService<ILoggerFactory>();

      // Setup null logger for baseline
      _NullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ComparisonBenchmarks>.Instance;
    }

    /// <summary>
    /// Cleanup method called after benchmark execution.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup() {
      _ServiceProvider?.Dispose();
      _MockFactory?.Dispose();
      _ConsoleFactory?.Dispose();
    }

    /// <summary>
    /// Setup method called before each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup() => _MockStore.Clear();

    /// <summary>
    /// Baseline benchmark using NullLogger (no-op).
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark(Baseline = true)]
    public int NullLogger_Baseline() {
      for (var i = 0; i < OperationCount; i++) {
        _NullLogger.LogInformation("Baseline message {Index} with some content", i);

        if (i % 100 == 0) {
          _NullLogger.LogWarning("Baseline warning {Index}", i);
        }
      }

      return OperationCount;
    }

    /// <summary>
    /// Benchmark for MockLogger performance.
    /// </summary>
    /// <returns>Number of entries stored for verification.</returns>
    [Benchmark]
    public int MockLogger_Performance() {
      for (var i = 0; i < OperationCount; i++) {
        _MockLogger.LogInformation("Mock message {Index} with some content", i);

        if (i % 100 == 0) {
          _MockLogger.LogWarning("Mock warning {Index}", i);
        }
      }

      return _MockStore.GetAll().Count;
    }

    /// <summary>
    /// Benchmark for standard console logger performance.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_Performance() {
      for (var i = 0; i < OperationCount; i++) {
        _ConsoleLogger.LogInformation("Console message {Index} with some content", i);

        if (i % 100 == 0) {
          _ConsoleLogger.LogWarning("Console warning {Index}", i);
        }
      }

      return OperationCount;
    }

    /// <summary>
    /// Benchmark comparing scope performance.
    /// </summary>
    /// <returns>Number of entries created for verification.</returns>
    [Benchmark]
    public int MockLogger_ScopeComparison() {
      for (var i = 0; i < OperationCount; i++) {
        using var scope = _MockLogger.BeginScope("Scope {Index}", i);
        _MockLogger.LogInformation("Scoped message {Index}", i);
      }

      return _MockStore.GetAll().Count;
    }

    /// <summary>
    /// Benchmark for console logger with scopes.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_ScopeComparison() {
      for (var i = 0; i < OperationCount; i++) {
        using var scope = _ConsoleLogger.BeginScope("Scope {Index}", i);
        _ConsoleLogger.LogInformation("Scoped message {Index}", i);
      }

      return OperationCount;
    }

    /// <summary>
    /// Benchmark comparing structured logging performance.
    /// </summary>
    /// <returns>Number of entries stored for verification.</returns>
    [Benchmark]
    public int MockLogger_StructuredComparison() {
      for (var i = 0; i < OperationCount; i++) {
        var state = new {
          Index = i,
          UserId = i % 1000,
          Timestamp = DateTimeOffset.UtcNow,
          Operation = "StructuredBenchmark"
        };

        _MockLogger.LogInformation("Structured message {Index} for user {UserId}", i, state.UserId);
      }

      return _MockStore.GetAll().Count;
    }

    /// <summary>
    /// Benchmark for console logger with structured logging.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_StructuredComparison() {
      for (var i = 0; i < OperationCount; i++) {
        var state = new {
          Index = i,
          UserId = i % 1000,
          Timestamp = DateTimeOffset.UtcNow,
          Operation = "StructuredBenchmark"
        };

        _ConsoleLogger.LogInformation("Structured message {Index} for user {UserId}", i, state.UserId);
      }

      return OperationCount;
    }

    /// <summary>
    /// Benchmark comparing exception logging performance.
    /// </summary>
    /// <returns>Number of entries stored for verification.</returns>
    [Benchmark]
    public int MockLogger_ExceptionComparison() {
      for (var i = 0; i < OperationCount; i++) {
        if (i % 100 == 0) {
          var exception = new InvalidOperationException($"Test exception {i}");
          _MockLogger.LogError(exception, "Exception occurred at index {Index}", i);
        } else {
          _MockLogger.LogInformation("Normal message {Index}", i);
        }
      }

      return _MockStore.GetAll().Count;
    }

    /// <summary>
    /// Benchmark for console logger with exceptions.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_ExceptionComparison() {
      for (var i = 0; i < OperationCount; i++) {
        if (i % 100 == 0) {
          var exception = new InvalidOperationException($"Test exception {i}");
          _ConsoleLogger.LogError(exception, "Exception occurred at index {Index}", i);
        } else {
          _ConsoleLogger.LogInformation("Normal message {Index}", i);
        }
      }

      return OperationCount;
    }

    /// <summary>
    /// Benchmark comparing factory-created logger performance.
    /// </summary>
    /// <returns>Number of entries stored for verification.</returns>
    [Benchmark]
    public int MockFactory_LoggerCreation() {
      var loggers = new ILogger[10];

      for (var i = 0; i < loggers.Length; i++) {
        loggers[i] = _MockFactory.CreateLogger($"TestCategory{i}");
      }

      for (var i = 0; i < OperationCount; i++) {
        var logger = loggers[i % loggers.Length];
        logger.LogInformation("Factory logger message {Index}", i);
      }

      return _MockStore.GetAll().Count;
    }

    /// <summary>
    /// Benchmark for console factory logger creation and usage.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleFactory_LoggerCreation() {
      var loggers = new ILogger[10];

      for (var i = 0; i < loggers.Length; i++) {
        loggers[i] = _ConsoleFactory.CreateLogger($"TestCategory{i}");
      }

      for (var i = 0; i < OperationCount; i++) {
        var logger = loggers[i % loggers.Length];
        logger.LogInformation("Factory logger message {Index}", i);
      }

      return OperationCount;
    }

    /// <summary>
    /// Benchmark comparing IsEnabled check performance.
    /// </summary>
    /// <returns>Number of enabled checks performed for verification.</returns>
    [Benchmark]
    public int MockLogger_IsEnabledComparison() {
      var enabledCount = 0;

      for (var i = 0; i < OperationCount; i++) {
        if (_MockLogger.IsEnabled(LogLevel.Information)) {
          _MockLogger.LogInformation("Enabled message {Index}", i);
          enabledCount++;
        }

        if (_MockLogger.IsEnabled(LogLevel.Debug)) {
          _MockLogger.LogDebug("Debug message {Index}", i);
          enabledCount++;
        }
      }

      return enabledCount;
    }

    /// <summary>
    /// Benchmark for console logger IsEnabled checks.
    /// </summary>
    /// <returns>Number of enabled checks performed for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_IsEnabledComparison() {
      var enabledCount = 0;

      for (var i = 0; i < OperationCount; i++) {
        if (_ConsoleLogger.IsEnabled(LogLevel.Information)) {
          _ConsoleLogger.LogInformation("Enabled message {Index}", i);
          enabledCount++;
        }

        if (_ConsoleLogger.IsEnabled(LogLevel.Debug)) {
          _ConsoleLogger.LogDebug("Debug message {Index}", i);
          enabledCount++;
        }
      }

      return enabledCount;
    }

    /// <summary>
    /// Benchmark comparing concurrent logging performance.
    /// </summary>
    /// <returns>Number of entries stored for verification.</returns>
    [Benchmark]
    public int MockLogger_ConcurrentComparison() {
      var tasks = new Task[Environment.ProcessorCount];

      for (var t = 0; t < tasks.Length; t++) {
        var threadId = t;
        tasks[t] = Task.Run(() => {
          var operationsPerThread = OperationCount / Environment.ProcessorCount;

          for (var i = 0; i < operationsPerThread; i++) {
            using var scope = _MockLogger.BeginScope("Thread {ThreadId} Operation {Index}", threadId, i);
            _MockLogger.LogInformation("Concurrent message from thread {ThreadId}", threadId);
          }
        });
      }

      Task.WaitAll(tasks);

      return _MockStore.GetAll().Count;
    }

    /// <summary>
    /// Benchmark for console logger concurrent performance.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_ConcurrentComparison() {
      var tasks = new Task[Environment.ProcessorCount];

      for (var t = 0; t < tasks.Length; t++) {
        var threadId = t;
        tasks[t] = Task.Run(() => {
          var operationsPerThread = OperationCount / Environment.ProcessorCount;

          for (var i = 0; i < operationsPerThread; i++) {
            using var scope = _ConsoleLogger.BeginScope("Thread {ThreadId} Operation {Index}", threadId, i);
            _ConsoleLogger.LogInformation("Concurrent message from thread {ThreadId}", threadId);
          }
        });
      }

      Task.WaitAll(tasks);

      return OperationCount;
    }

    /// <summary>
    /// Comprehensive real-world scenario comparison.
    /// </summary>
    /// <returns>Number of entries stored for verification.</returns>
    [Benchmark]
    public int MockLogger_RealWorldScenario() => ExecuteRealWorldScenario(_MockLogger);

    /// <summary>
    /// Real-world scenario with console logger.
    /// </summary>
    /// <returns>Operation count for verification.</returns>
    [Benchmark]
    public int ConsoleLogger_RealWorldScenario() {
      ExecuteRealWorldScenario(_ConsoleLogger);
      return OperationCount;
    }

    /// <summary>
    /// Executes a realistic logging scenario.
    /// </summary>
    /// <param name="logger">Logger to use for the scenario.</param>
    /// <returns>Number of operations performed.</returns>
    private int ExecuteRealWorldScenario(ILogger logger) {
      var operations = 0;
      var random = new Random(42); // Fixed seed for consistency

      for (var i = 0; i < OperationCount; i++) {
        var requestId = Guid.NewGuid();
        var userId = random.Next(1, 1001);

        using var requestScope = logger.BeginScope(new Dictionary<string, object> {
          { "RequestId", requestId },
          { "UserId", userId },
          { "Operation", "ProcessRequest" }
        });

        logger.LogInformation("Request {RequestId} started for user {UserId}", requestId, userId);
        operations++;

        // Simulate various operations
        var operationType = random.Next(4);

        switch (operationType) {
          case 0:
            // Simple operation
            logger.LogInformation("Processing simple operation for request {RequestId}", requestId);
            operations++;
            break;

          case 1:
            // Operation with validation
            using (var validationScope = logger.BeginScope("Validation")) {
              logger.LogDebug("Validating request {RequestId}", requestId);
              operations++;

              if (random.Next(10) == 0) {
                logger.LogWarning("Validation warning for request {RequestId}", requestId);
                operations++;
              }
            }
            break;

          case 2:
            // Operation with database access
            using (var dbScope = logger.BeginScope("DatabaseOperation")) {
              logger.LogDebug("Accessing database for request {RequestId}", requestId);
              operations++;

              if (random.Next(50) == 0) {
                var exception = new InvalidOperationException("Database connection failed");
                logger.LogError(exception, "Database error for request {RequestId}", requestId);
                operations++;
              }
            }
            break;

          case 3:
            // Complex operation with multiple steps
            using (var complexScope = logger.BeginScope("ComplexOperation")) {
              logger.LogInformation("Starting complex operation for request {RequestId}", requestId);
              operations++;

              for (var step = 0; step < 3; step++) {
                using var stepScope = logger.BeginScope($"Step{step}");
                logger.LogDebug("Executing step {Step} for request {RequestId}", step, requestId);
                operations++;
              }

              logger.LogInformation("Complex operation completed for request {RequestId}", requestId);
              operations++;
            }
            break;
        }

        logger.LogInformation("Request {RequestId} completed successfully", requestId);
        operations++;
      }

      return operations;
    }

    public void Dispose() {
      _MockFactory?.Dispose();
      _ConsoleFactory?.Dispose();
      _ServiceProvider?.Dispose();
    }
  }
}
