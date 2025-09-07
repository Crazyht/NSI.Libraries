using System.Diagnostics.CodeAnalysis;

namespace NSI.Testing.Benchmarks;
/// <summary>
/// Benchmarks for memory usage and allocation patterns in MockLogger.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure memory allocation patterns, garbage collection impact,
/// and memory efficiency of various MockLogger operations.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class MemoryPerformanceBenchmarks {
  private ILogEntryStore _Store = null!;
  private ILogger<MemoryPerformanceBenchmarks> _Logger = null!;

  /// <summary>
  /// Number of operations for memory testing.
  /// </summary>
  [Params(1000, 10000)]
  public int OperationCount { get; set; }

  /// <summary>
  /// Size of state objects for memory allocation testing.
  /// </summary>
  [Params(10, 100, 1000)]
  public int StateObjectSize { get; set; }

  /// <summary>
  /// Setup method called before each benchmark run.
  /// </summary>
  [GlobalSetup]
  public void GlobalSetup() {
    _Store = new InMemoryLogEntryStore();
    _Logger = new MockLogger<MemoryPerformanceBenchmarks>(_Store);
  }

  /// <summary>
  /// Setup method called before each benchmark iteration.
  /// </summary>
  [IterationSetup]
  public void IterationSetup() {
    _Store.Clear();

    // Force garbage collection to get clean measurements
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
  }

  /// <summary>
  /// Benchmark for basic memory allocation patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark(Baseline = true)]
  public int Memory_BasicLogging() {
    for (var i = 0; i < OperationCount; i++) {
      _Logger.LogInformation("Basic message {Index} with some content", i);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for string interpolation memory impact.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_StringInterpolation() {
    for (var i = 0; i < OperationCount; i++) {
      var userId = i % 1000;
      var timestamp = DateTimeOffset.UtcNow;
      const string? operation = "ProcessUser";
      _Logger.LogInformation($"User {userId} performing {operation} at {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for structured logging memory patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_StructuredLogging() {
    for (var i = 0; i < OperationCount; i++) {
      var state = new {
        Index = i,
        UserId = i % 1000,
        Timestamp = DateTimeOffset.UtcNow,
        Operation = "BenchmarkOperation"
      };

      _Logger.LogInformation("Structured message {Index}", state);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for large state object memory impact.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_LargeStateObjects() {
    for (var i = 0; i < OperationCount; i++) {
      var largeState = CreateLargeStateObject(i);
      _Logger.LogInformation("Large state message {Index}", largeState);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for scope memory allocation patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_ScopeAllocation() {
    for (var i = 0; i < OperationCount; i++) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "ScopeId", i },
        { "Timestamp", DateTimeOffset.UtcNow },
        { "Operation", "MemoryBenchmark" }
      });

      _Logger.LogInformation("Message in scope {Index}", i);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for nested scope memory patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_NestedScopes() {
    const int nestingDepth = 5;

    for (var i = 0; i < OperationCount; i++) {
      CreateNestedScopesWithLogging(nestingDepth, i);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for exception memory allocation.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_ExceptionLogging() {
    for (var i = 0; i < OperationCount; i++) {
      var exception = new InvalidOperationException($"Test exception {i} with detailed message and stack trace information");

      _Logger.LogError(exception, "Exception occurred during operation {Index}", i);

    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for array state memory patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_ArrayState() {
    for (var i = 0; i < OperationCount; i++) {
      var arrayState = new object[] {
        $"Item {i}",
        i,
        DateTimeOffset.UtcNow,
        new { UserId = i % 1000, Operation = "ArrayBenchmark" },
        Enumerable.Range(0, 5).Select(x => $"SubItem_{x}").ToArray()
      };

      _Logger.LogInformation("Array state message {Index}", arrayState);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for dictionary state memory patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_DictionaryState() {
    for (var i = 0; i < OperationCount; i++) {
      var dictionaryState = new Dictionary<string, object> {
        ["Index"] = i,
        ["UserId"] = i % 1000,
        ["Timestamp"] = DateTimeOffset.UtcNow,
        ["Operation"] = "DictionaryBenchmark",
        ["Metadata"] = new Dictionary<string, object> {
          ["Version"] = "1.0.0",
          ["Environment"] = "Benchmark"
        },
        ["Items"] = Enumerable.Range(0, StateObjectSize / 10).Select(x => $"Item_{x}").ToArray()
      };

      _Logger.LogInformation("Dictionary state message {Index}", dictionaryState);
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for formatter function memory impact.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_FormatterFunctions() {
    for (var i = 0; i < OperationCount; i++) {
      var state = $"State object {i}";
      var exception = i % 100 == 0 ? new InvalidOperationException($"Error {i}") : null;

      // Different formatter patterns to test memory allocation
      if (i % 3 == 0) {
        _Logger.Log(LogLevel.Information, new EventId(i), state, exception,
          (s, _) => $"Simple format: {s}");
      } else if (i % 3 == 1) {
        _Logger.Log(LogLevel.Information, new EventId(i), state, exception,
          (s, ex) => ex != null ? $"With exception: {s} - {ex.Message}" : $"Without exception: {s}");
      } else {
        _Logger.Log(LogLevel.Information, new EventId(i), state, exception,
          (s, ex) => $"Complex format at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: {s} (Exception: {ex?.GetType().Name ?? "None"})");
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for concurrent memory allocation patterns.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_ConcurrentAllocation() {
    var tasks = new Task[Environment.ProcessorCount];

    for (var t = 0; t < tasks.Length; t++) {
      var threadId = t;
      tasks[t] = Task.Run(() => {
        var operationsPerThread = OperationCount / Environment.ProcessorCount;

        for (var i = 0; i < operationsPerThread; i++) {
          using var scope = _Logger.BeginScope(new Dictionary<string, object> {
            { "ThreadId", threadId },
            { "Index", i },
            { "Timestamp", DateTimeOffset.UtcNow }
          });

          var state = new {
            ThreadId = threadId,
            Index = i,
            Data = Enumerable.Range(0, 10).Select(x => $"ThreadData_{threadId}_{x}").ToArray()
          };

          _Logger.LogInformation("Concurrent message from thread {ThreadId}", state);
        }
      });
    }

    Task.WaitAll(tasks);

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for memory pressure under high load.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_HighLoadPressure() {
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    for (var i = 0; i < OperationCount; i++) {
      // Simulate varying memory pressure
      var pressureLevel = i % 3;

      if (pressureLevel == 0) {
        // Low pressure - simple message
        _Logger.LogInformation("Simple message {Index}", i);
      } else if (pressureLevel == 1) {
        // Medium pressure - structured logging
        var state = new {
          Index = i,
          Timestamp = DateTimeOffset.UtcNow,
          ElapsedMs = stopwatch.ElapsedMilliseconds
        };
        _Logger.LogInformation("Medium complexity message {Index}", state);
      } else {
        // High pressure - complex state with scope
        using var scope = _Logger.BeginScope(CreateLargeStateObject(i));

        var complexState = new Dictionary<string, object> {
          ["Index"] = i,
          ["ComplexData"] = Enumerable.Range(0, StateObjectSize / 20)
            .Select(x => new {
              Id = x,
              Value = $"ComplexValue_{i}_{x}",
              Timestamp = DateTimeOffset.UtcNow.AddMilliseconds(x)
            }).ToArray()
        };

        _Logger.LogInformation("High complexity message {Index}", complexState);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for store memory patterns.
  /// </summary>
  /// <returns>Number of entries in store for verification.</returns>
  [Benchmark]
  public int Memory_StoreGrowthPatterns() {
    // Test memory patterns as store grows
    for (var batch = 0; batch < 10; batch++) {
      var batchSize = OperationCount / 10;

      for (var i = 0; i < batchSize; i++) {
        using var scope = _Logger.BeginScope(new Dictionary<string, object> {
          { "Batch", batch },
          { "Item", i },
          { "TotalItems", _Store.GetAll().Count }
        });

        _Logger.LogInformation("Batch {Batch} item {Item}", batch, i);

        // Occasionally check store size (simulating real usage)
        if (i % 100 == 0) {
          var currentSize = _Store.GetAll().Count;
          _Logger.LogDebug("Store contains {Count} entries", currentSize);
        }
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for garbage collection impact.
  /// </summary>
  /// <returns>Number of entries created for verification.</returns>
  [Benchmark]
  public int Memory_GarbageCollectionImpact() {
    var gen0Before = GC.CollectionCount(0);
    var gen1Before = GC.CollectionCount(1);
    var gen2Before = GC.CollectionCount(2);

    for (var i = 0; i < OperationCount; i++) {
      // Create temporary objects that will need garbage collection
      var temporaryData = new List<object>();

      for (var j = 0; j < 10; j++) {
        temporaryData.Add(new {
          Index = j,
          Data = $"Temporary data {i}_{j}",
          Timestamp = DateTimeOffset.UtcNow
        });
      }

      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "Index", i },
        { "TemporaryItemCount", temporaryData.Count }
      });

      _Logger.LogInformation("Processing with temporary data {Index}", i);

      // Let temporaryData go out of scope and become eligible for GC
    }

    var gen0After = GC.CollectionCount(0);
    var gen1After = GC.CollectionCount(1);
    var gen2After = GC.CollectionCount(2);

    // Log GC stats for analysis
    _Logger.LogInformation("GC Stats - Gen0: {Gen0Collections}, Gen1: {Gen1Collections}, Gen2: {Gen2Collections}",
      gen0After - gen0Before, gen1After - gen1Before, gen2After - gen2Before);

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Creates a large state object for memory testing.
  /// </summary>
  /// <param name="index">Index for unique values.</param>
  /// <returns>Dictionary with large amount of data.</returns>
  private Dictionary<string, object> CreateLargeStateObject(int index) {
    var largeState = new Dictionary<string, object> {
      ["Index"] = index,
      ["Timestamp"] = DateTimeOffset.UtcNow,
      ["UserId"] = index % 1000,
      ["SessionId"] = Guid.NewGuid(),
      ["RequestId"] = Guid.NewGuid()
    };

    // Add variable number of properties based on StateObjectSize
    for (var i = 0; i < StateObjectSize; i++) {
      largeState[$"Property_{i}"] = $"Value_{index}_{i}_{DateTimeOffset.UtcNow.Ticks % 10000}";
    }

    // Add some complex nested objects
    largeState["NestedObject"] = new {
      Id = index,
      Values = Enumerable.Range(0, Math.Min(StateObjectSize / 10, 100))
        .Select(x => $"NestedValue_{x}")
        .ToArray(),
      Metadata = new Dictionary<string, object> {
        ["CreatedAt"] = DateTimeOffset.UtcNow,
        ["Source"] = "MemoryBenchmark",
        ["Version"] = "1.0.0"
      }
    };

    return largeState;
  }

  /// <summary>
  /// Creates nested scopes with logging for memory testing.
  /// </summary>
  /// <param name="depth">Remaining depth to create.</param>
  /// <param name="baseIndex">Base index for unique values.</param>
  private void CreateNestedScopesWithLogging(int depth, int baseIndex) {
    if (depth <= 0) {
      return;
    }

    using var scope = _Logger.BeginScope(new Dictionary<string, object> {
      { "Depth", depth },
      { "BaseIndex", baseIndex },
      { "ScopeId", Guid.NewGuid() },
      { "Timestamp", DateTimeOffset.UtcNow }
    });

    _Logger.LogDebug("At nesting level {Depth} for base {BaseIndex}", depth, baseIndex);

    // Create some temporary objects at each level
    var tempData = Enumerable.Range(0, 5)
      .Select(x => new { Level = depth, Item = x, Data = $"TempData_{depth}_{x}" })
      .ToArray();

    _Logger.LogInformation("Created {Count} temporary objects at level {Depth}", tempData.Length, depth);

    CreateNestedScopesWithLogging(depth - 1, baseIndex);
  }
}
