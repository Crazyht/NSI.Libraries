using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace NSI.Testing.Benchmarks;
/// <summary>
/// Benchmarks for basic logging performance in MockLogger.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the core logging performance characteristics
/// including message formatting, storage operations, and memory allocation patterns.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class LoggingPerformanceBenchmarks {
  private ILogEntryStore _Store = null!;
  private ILogger<LoggingPerformanceBenchmarks> _MockLogger = null!;
  private ILogger<LoggingPerformanceBenchmarks> _ConsoleLogger = null!;
  private ServiceProvider _ServiceProvider = null!;

  /// <summary>
  /// Number of log entries to generate for high-volume tests.
  /// </summary>
  [Params(100, 1000, 10000)]
  public int LogCount { get; set; }

  /// <summary>
  /// Log level to use for benchmarks.
  /// </summary>
  [Params(LogLevel.Debug, LogLevel.Information, LogLevel.Error)]
  public LogLevel Level { get; set; }

  /// <summary>
  /// Setup method called before each benchmark run.
  /// </summary>
  [GlobalSetup]
  public void GlobalSetup() {
    // Setup MockLogger
    _Store = new InMemoryLogEntryStore();
    _MockLogger = new MockLogger<LoggingPerformanceBenchmarks>(_Store);

    // Setup standard console logger for comparison
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
    _ServiceProvider = services.BuildServiceProvider();
    _ConsoleLogger = _ServiceProvider.GetRequiredService<ILogger<LoggingPerformanceBenchmarks>>();
  }

  /// <summary>
  /// Cleanup method called after benchmark execution.
  /// </summary>
  [GlobalCleanup]
  public void GlobalCleanup() => _ServiceProvider?.Dispose();

  /// <summary>
  /// Setup method called before each benchmark iteration.
  /// </summary>
  [IterationSetup]
  public void IterationSetup() => _Store.Clear();

  /// <summary>
  /// Benchmark for basic MockLogger logging performance.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark(Baseline = true)]
  public int MockLogger_BasicLogging() {
    for (var i = 0; i < LogCount; i++) {
      _MockLogger.Log(
        Level,
        new EventId(i),
        $"Benchmark message {i} with some additional text for realistic message length",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? ""
      );
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for MockLogger with structured logging state.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int MockLogger_StructuredLogging() {
    for (var i = 0; i < LogCount; i++) {
      var state = new {
        MessageId = i,
        Timestamp = DateTimeOffset.UtcNow,
        UserId = 12345,
        Operation = "BenchmarkOperation",
        Duration = TimeSpan.FromMilliseconds(i % 1000)
      };

      _MockLogger.Log(Level, new EventId(i), state, null, (s, _) => $"Structured message {i}: {s}");
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for MockLogger with exception logging.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int MockLogger_WithExceptions() {
    for (var i = 0; i < LogCount; i++) {
      var exception = i % 10 == 0 ? new InvalidOperationException($"Test exception {i}") : null;
      _MockLogger.Log(
        Level,
        new EventId(i),
        $"Message {i} with potential exception",
        exception,
        (s, ex) => ex != null
          ? $"{s}: {ex.Message}"
          : s?.ToString(CultureInfo.InvariantCulture) ?? ""
      );
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark comparison with standard console logger.
  /// </summary>
  [Benchmark]
  public void ConsoleLogger_Baseline() {
    for (var i = 0; i < LogCount; i++) {
      _ConsoleLogger.Log(
        Level,
        new EventId(i),
        $"Baseline message {i} with some additional text for realistic message length",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? ""
      );
    }
  }

  /// <summary>
  /// Benchmark for logging with string interpolation.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int MockLogger_StringInterpolation() {
    for (var i = 0; i < LogCount; i++) {
      _MockLogger.Log(
        Level,
        new EventId(i),
        $"Interpolated message {i} at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} for user {12345}",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? ""
      );
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for high-frequency logging with minimal message content.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int MockLogger_HighFrequency() {
    for (var i = 0; i < LogCount; i++) {
      _MockLogger.Log(
        Level,
        new EventId(i),
        $"Msg{i}",
        null,
        (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? ""
      );
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for logging with complex state objects.
  /// </summary>
  /// <returns>Number of entries stored for verification.</returns>
  [Benchmark]
  public int MockLogger_ComplexState() {
    for (var i = 0; i < LogCount; i++) {
      var complexState = new Dictionary<string, object> {
        ["Id"] = i,
        ["Timestamp"] = DateTimeOffset.UtcNow,
        ["UserData"] = new { Name = $"User{i}", Email = $"user{i}@example.com" },
        ["Metrics"] = new { Duration = i * 10, BytesProcessed = i * 1024 },
        ["Tags"] = new[] { "benchmark", "performance", $"iteration-{i}" }
      };

      _MockLogger.Log(Level, new EventId(i), complexState, null, (_, _) => $"Complex state message {i}");
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for IsEnabled checks performance.
  /// </summary>
  /// <returns>Number of enabled checks performed.</returns>
  [Benchmark]
  public int MockLogger_IsEnabledChecks() {
    var enabledCount = 0;
    for (var i = 0; i < LogCount; i++) {
      if (_MockLogger.IsEnabled(Level)) {
        enabledCount++;
      }
    }

    return enabledCount;
  }
}
