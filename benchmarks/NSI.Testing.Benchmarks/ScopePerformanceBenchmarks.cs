using System.Diagnostics.CodeAnalysis;

namespace NSI.Testing.Benchmarks;
/// <summary>
/// Benchmarks for scope management performance in MockLogger.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the performance characteristics of scope operations
/// including creation, disposal, nesting, and memory allocation patterns.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class ScopePerformanceBenchmarks {
  private ILogEntryStore _Store = null!;
  private ILogger<ScopePerformanceBenchmarks> _Logger = null!;

  /// <summary>
  /// Number of scopes to create for testing.
  /// </summary>
  [Params(100, 1000, 5000)]
  public int ScopeCount { get; set; }

  /// <summary>
  /// Depth of nested scopes for hierarchy tests.
  /// </summary>
  [Params(1, 10, 50)]
  public int NestingDepth { get; set; }

  /// <summary>
  /// Setup method called before each benchmark run.
  /// </summary>
  [GlobalSetup]
  public void GlobalSetup() {
    _Store = new InMemoryLogEntryStore();
    _Logger = new MockLogger<ScopePerformanceBenchmarks>(_Store);
  }

  /// <summary>
  /// Setup method called before each benchmark iteration.
  /// </summary>
  [IterationSetup]
  public void IterationSetup() => _Store.Clear();

  /// <summary>
  /// Benchmark for basic scope creation and disposal.
  /// </summary>
  /// <returns>Number of scope entries created for verification.</returns>
  [Benchmark(Baseline = true)]
  public int BasicScopes_CreateAndDispose() {
    for (var i = 0; i < ScopeCount; i++) {
      using var scope = _Logger.BeginScope($"Scope {i}");
    }

    return _Store.GetAll().Count(e => e.Type != EntryType.Log);
  }

  /// <summary>
  /// Benchmark for scopes with structured state.
  /// </summary>
  /// <returns>Number of scope entries created for verification.</returns>
  [Benchmark]
  public int StructuredScopes_WithVariables() {
    for (var i = 0; i < ScopeCount; i++) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "ScopeId", i },
        { "Timestamp", DateTimeOffset.UtcNow },
        { "UserId", 12345 },
        { "Operation", "BenchmarkOperation" }
      });
    }

    return _Store.GetAll().Count(e => e.Type != EntryType.Log);
  }

  /// <summary>
  /// Benchmark for nested scope hierarchies.
  /// </summary>
  /// <returns>Number of scope entries created for verification.</returns>
  [Benchmark]
  public int NestedScopes_HierarchyManagement() {
    CreateNestedScopes(NestingDepth);
    return _Store.GetAll().Count(e => e.Type != EntryType.Log);
  }

  /// <summary>
  /// Benchmark for scopes with logging activity.
  /// </summary>
  /// <returns>Total number of entries created for verification.</returns>
  [Benchmark]
  public int ScopesWithLogging_CombinedActivity() {
    for (var i = 0; i < ScopeCount; i++) {
      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "ScopeId", i },
        { "BatchSize", ScopeCount }
      });

      _Logger.LogInformation("Processing item {ItemId} in scope {ScopeId}", i, i);
      _Logger.LogDebug("Debug information for item {ItemId}", i);

      if (i % 100 == 0) {
        _Logger.LogWarning("Checkpoint reached at item {ItemId}", i);
      }
    }

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for complex nested scopes with multiple logs.
  /// </summary>
  /// <returns>Total number of entries created for verification.</returns>
  [Benchmark]
  public int ComplexNestedScopes_WithLogging() {
    using var outerScope = _Logger.BeginScope(new Dictionary<string, object> {
      { "Operation", "ComplexBenchmark" },
      { "TotalItems", ScopeCount }
    });

    _Logger.LogInformation("Starting complex nested scope benchmark");

    for (var i = 0; i < ScopeCount; i++) {
      using var middleScope = _Logger.BeginScope(new Dictionary<string, object> {
        { "BatchId", i / 100 },
        { "ItemId", i }
      });

      _Logger.LogDebug("Processing batch item {ItemId}", i);

      if (i % 10 == 0) {
        using var innerScope = _Logger.BeginScope(new Dictionary<string, object> {
          { "SpecialOperation", true },
          { "Timestamp", DateTimeOffset.UtcNow }
        });

        _Logger.LogInformation("Special processing for item {ItemId}", i);
      }
    }

    _Logger.LogInformation("Complex nested scope benchmark completed");

    return _Store.GetAll().Count;
  }

  /// <summary>
  /// Benchmark for scope disposal timing.
  /// </summary>
  /// <returns>Number of scope end entries for verification.</returns>
  [Benchmark]
  public int ScopeDisposal_Timing() {
    var scopes = new List<IDisposable>();

    // Create all scopes first
    for (var i = 0; i < ScopeCount; i++) {
      scopes.Add(_Logger.BeginScope($"Scope {i}")!);
    }

    // Dispose all scopes (timing this operation)
    foreach (var scope in scopes) {
      scope.Dispose();
    }

    return _Store.GetAll().Count(e => e.Type == EntryType.ScopeEnd);
  }

  /// <summary>
  /// Benchmark for large scope state objects.
  /// </summary>
  /// <returns>Number of scope entries created for verification.</returns>
  [Benchmark]
  public int LargeScopeState_MemoryImpact() {
    for (var i = 0; i < ScopeCount; i++) {
      var largeState = new Dictionary<string, object>();

      // Create large state object with many variables
      for (var j = 0; j < 20; j++) {
        largeState[$"Variable_{j}"] = $"Value_{i}_{j}_{DateTimeOffset.UtcNow.Ticks}";
      }

      largeState["ComplexObject"] = new {
        Id = i,
        Data = Enumerable.Range(0, 10).Select(x => $"Item_{x}").ToArray(),
        Metadata = new Dictionary<string, object> {
          ["CreatedAt"] = DateTimeOffset.UtcNow,
          ["Version"] = "1.0.0",
          ["Tags"] = new[] { "benchmark", "performance", "large-state" }
        }
      };

      using var scope = _Logger.BeginScope(largeState);
    }

    return _Store.GetAll().Count(e => e.Type != EntryType.Log);
  }

  /// <summary>
  /// Benchmark for concurrent scope operations.
  /// </summary>
  /// <returns>Number of scope entries created for verification.</returns>
  [Benchmark]
  public int ConcurrentScopes_ThreadSafety() {
    var tasks = new Task[Environment.ProcessorCount];

    for (var t = 0; t < tasks.Length; t++) {
      var threadId = t;
      tasks[t] = Task.Run(() => {
        var scopesPerThread = ScopeCount / Environment.ProcessorCount;

        for (var i = 0; i < scopesPerThread; i++) {
          using var scope = _Logger.BeginScope(new Dictionary<string, object> {
            { "ThreadId", threadId },
            { "ScopeIndex", i },
            { "ConcurrentTest", true }
          });

          _Logger.LogInformation("Concurrent scope {ScopeIndex} on thread {ThreadId}", i, threadId);
        }
      });
    }

    Task.WaitAll(tasks);

    return _Store.GetAll().Count(e => e.Type != EntryType.Log);
  }

  /// <summary>
  /// Helper method to create nested scopes recursively.
  /// </summary>
  /// <param name="depth">Remaining depth to create.</param>
  private void CreateNestedScopes(int depth) {
    if (depth <= 0) {
      return;
    }

    using var scope = _Logger.BeginScope(new Dictionary<string, object> {
      { "Level", NestingDepth - depth + 1 },
      { "RemainingDepth", depth }
    });

    _Logger.LogDebug("At nesting level {Level}", NestingDepth - depth + 1);
    CreateNestedScopes(depth - 1);
  }
}
