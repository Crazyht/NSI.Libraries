using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NSI.Testing.Benchmarks;
/// <summary>
/// Benchmarks for LINQ query performance on MockLogger data.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the performance characteristics of various LINQ queries
/// on log entry collections, including filtering, grouping, and complex analysis operations.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class QueryPerformanceBenchmarks {
  private ILogEntryStore _Store = null!;
  private ILogger<QueryPerformanceBenchmarks> _Logger = null!;
  private IReadOnlyList<LogEntry> _TestData = null!;

  /// <summary>
  /// Size of the dataset for query performance testing.
  /// </summary>
  [Params(1000, 10000, 50000)]
  public int DatasetSize { get; set; }

  /// <summary>
  /// Setup method called before each benchmark run.
  /// </summary>
  [GlobalSetup]
  public void GlobalSetup() {
    _Store = new InMemoryLogEntryStore();
    _Logger = new MockLogger<QueryPerformanceBenchmarks>(_Store);

    GenerateTestData();
    _TestData = _Store.GetAll();
  }

  /// <summary>
  /// Benchmark for basic type filtering queries.
  /// </summary>
  /// <returns>Number of filtered entries for verification.</returns>
  [Benchmark(Baseline = true)]
  public int Query_BasicTypeFiltering() {
    var logEntries = _TestData.LogsOnly().Count();
    var scopeStarts = _TestData.ScopeStartsOnly().Count();
    var scopeEnds = _TestData.ScopeEndsOnly().Count();

    return logEntries + scopeStarts + scopeEnds;
  }

  /// <summary>
  /// Benchmark for log level filtering queries.
  /// </summary>
  /// <returns>Number of filtered entries for verification.</returns>
  [Benchmark]
  public int Query_LogLevelFiltering() {
    var errors = _TestData.WithLogLevel(LogLevel.Error).Count();
    var warnings = _TestData.WithLogLevel(LogLevel.Warning).Count();
    var information = _TestData.WithLogLevel(LogLevel.Information).Count();

    return errors + warnings + information;
  }

  /// <summary>
  /// Benchmark for message content filtering.
  /// </summary>
  /// <returns>Number of filtered entries for verification.</returns>
  [Benchmark]
  public int Query_MessageFiltering() {
    var containsUser = _TestData.WithMessageContains("User").Count();
    var startsWithProcess = _TestData.WithMessageStartsWith("Process").Count();
    var endsWithCompleted = _TestData.WithMessageEndsWith("completed").Count();

    return containsUser + startsWithProcess + endsWithCompleted;
  }

  /// <summary>
  /// Benchmark for regular expression message matching.
  /// </summary>
  /// <returns>Number of matching entries for verification.</returns>
  [Benchmark]
  public int Query_RegexMatching() {
    var emailPattern = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    var numberPattern = new Regex(@"\b\d{4,}\b", RegexOptions.Compiled);

    var emailMatches = _TestData.WithMessageMatch(emailPattern).Count();
    var numberMatches = _TestData.WithMessageMatch(numberPattern).Count();

    return emailMatches + numberMatches;
  }

  /// <summary>
  /// Benchmark for scope variable filtering.
  /// </summary>
  /// <returns>Number of filtered scope entries for verification.</returns>
  [Benchmark]
  public int Query_ScopeFiltering() {
    var userScopes = _TestData.WithScopeContainingKey("UserId").Count();
    var operationScopes = _TestData.WithScopeContainingVar("Operation", "ProcessUser").Count();
    var batchScopes = _TestData.WithScopeContainingKey("BatchId").Count();

    return userScopes + operationScopes + batchScopes;
  }

  /// <summary>
  /// Benchmark for complex LINQ grouping operations.
  /// </summary>
  /// <returns>Number of groups created for verification.</returns>
  [Benchmark]
  public int Query_ComplexGrouping() {
    var levelGroups = _TestData
      .LogsOnly()
      .GroupBy(e => e.Level)
      .Count();

    var timeGroups = _TestData
      .LogsOnly()
      .GroupBy(e => e.EventId?.Id / 1000)
      .Count();

    var messageGroups = _TestData
      .LogsOnly()
      .Where(e => !string.IsNullOrEmpty(e.Message))
      .GroupBy(e => e.Message!.Split(' ').FirstOrDefault() ?? "Unknown")
      .Count();

    return levelGroups + timeGroups + messageGroups;
  }

  /// <summary>
  /// Benchmark for scope hierarchy analysis.
  /// </summary>
  /// <returns>Number of hierarchical relationships found for verification.</returns>
  [Benchmark]
  public int Query_ScopeHierarchyAnalysis() {
    var rootScopes = _TestData
      .ScopeStartsOnly()
      .Count(s => s.ParentScopeId == null);

    var nestedScopes = _TestData
      .ScopeStartsOnly()
      .Count(s => s.ParentScopeId != null);

    var scopeDepthAnalysis = _TestData
      .ScopeStartsOnly()
      .GroupBy(s => s.ParentScopeId == null ? 0 : 1)
      .Count();

    return rootScopes + nestedScopes + scopeDepthAnalysis;
  }

  /// <summary>
  /// Benchmark for exception analysis queries.
  /// </summary>
  /// <returns>Number of exception-related entries for verification.</returns>
  [Benchmark]
  public int Query_ExceptionAnalysis() {
    var withExceptions = _TestData.WithException().Count();
    var invalidOpExceptions = _TestData.WithException<InvalidOperationException>().Count();
    var argumentExceptions = _TestData.WithException<ArgumentException>().Count();

    return withExceptions + invalidOpExceptions + argumentExceptions;
  }

  /// <summary>
  /// Benchmark for complex multi-step analysis.
  /// </summary>
  /// <returns>Number of analysis results for verification.</returns>
  [Benchmark]
  public int Query_ComplexAnalysis() {
    var analysis = _TestData
      .LogsOnly()
      .Where(e => e.Level == LogLevel.Error || e.Level == LogLevel.Warning)
      .GroupBy(e => e.Level)
      .SelectMany(g => g
        .GroupBy(e => e.Message?.Split(' ').FirstOrDefault() ?? "Unknown")
        .Where(mg => mg.Count() > 5)
        .Select(mg => new { Level = g.Key, MessagePattern = mg.Key, Count = mg.Count() }))
      .OrderByDescending(x => x.Count)
      .Take(10)
      .Count();

    return analysis;
  }

  /// <summary>
  /// Benchmark for scope-to-log correlation queries.
  /// </summary>
  /// <returns>Number of correlated entries for verification.</returns>
  [Benchmark]
  public int Query_ScopeLogCorrelation() {
    var correlatedEntries = _TestData
      .ScopeStartsOnly()
      .WithScopeContainingKey("UserId")
      .SelectMany(scope => _TestData
        .WithinScope(scope.ScopeId!.Value)
        .LogsOnly())
      .Count();

    return correlatedEntries;
  }

  /// <summary>
  /// Benchmark for performance-sensitive filtering chains.
  /// </summary>
  /// <returns>Number of filtered entries for verification.</returns>
  [Benchmark]
  public int Query_FilteringChains() {
    var result1 = _TestData
      .LogsOnly()
      .WithLogLevel(LogLevel.Information)
      .WithMessageContains("User")
      .Count(e => e.EventId?.Id > 100);

    var result2 = _TestData
      .ScopeStartsOnly()
      .WithScopeContainingKey("BatchId")
      .Count(s => s.State.OfType<KeyValuePair<string, object>>()
        .Any(kv => kv.Key == "BatchId" && (int)kv.Value > 50));

    return result1 + result2;
  }

  /// <summary>
  /// Benchmark for large result set processing.
  /// </summary>
  /// <returns>Number of processed entries for verification.</returns>
  [Benchmark]
  public int Query_LargeResultSetProcessing() {
    var processedCount = 0;

    foreach (var entry in _TestData.LogsOnly().Where(e => e.Level >= LogLevel.Information)) {
      processedCount++;

      // Simulate processing work
      var messageLength = entry.Message?.Length ?? 0;
      var hasException = entry.Exception != null;
      var hasEventId = entry.EventId.HasValue;

      if (messageLength > 10 && hasEventId && !hasException) {
        // Count entries matching criteria
      }
    }

    return processedCount;
  }

  /// <summary>
  /// Benchmark for aggregation operations.
  /// </summary>
  /// <returns>Sum of aggregated values for verification.</returns>
  [Benchmark]
  public int Query_AggregationOperations() {
    var totalEntries = _TestData.Count;
    var logEntries = _TestData.LogsOnly().Count();
    var avgEventId = _TestData.LogsOnly()
      .Where(e => e.EventId.HasValue)
      .Select(e => e.EventId!.Value.Id)
      .DefaultIfEmpty(0)
      .Average();

    var maxScopeDepth = _TestData
      .ScopeStartsOnly()
      .Select(s => CalculateScopeDepth(s, _TestData))
      .DefaultIfEmpty(0)
      .Max();

    return totalEntries + logEntries + (int)avgEventId + maxScopeDepth;
  }

  /// <summary>
  /// Generates test data for benchmarking.
  /// </summary>
  private void GenerateTestData() {
    var random = new Random(42); // Fixed seed for reproducible results

    for (var i = 0; i < DatasetSize; i++) {
      var userId = random.Next(1, 101);
      var batchId = i / 100;
      var level = (LogLevel)random.Next(1, 7);

      using var scope = _Logger.BeginScope(new Dictionary<string, object> {
        { "UserId", userId },
        { "BatchId", batchId },
        { "Operation", random.Next(3) switch {
            0 => "ProcessUser",
            1 => "ValidateData",
            _ => "GenerateReport"
          }
        },
        { "Timestamp", DateTimeOffset.UtcNow.AddMilliseconds(i) }
      });

      // Generate various types of log messages
      var messageType = random.Next(5);
      var message = messageType switch {
        0 => $"User {userId} processed successfully",
        1 => $"Processing batch {batchId} item {i}",
        2 => $"Validation completed for user{userId}@example.com",
        3 => $"Report generated with {random.Next(100, 10000)} records",
        _ => $"General operation {i} completed"
      };

      Exception? exception = null;
      if (random.Next(20) == 0) { // 5% chance of exception
        exception = random.Next(3) switch {
          0 => new InvalidOperationException($"Test error {i}"),
          1 => new ArgumentException($"Invalid argument {i}"),
          _ => new TimeoutException($"Timeout error {i}")
        };
      }

      _Logger.Log(
        level,
        new EventId(i, $"Event{i}"),
        message,
        exception,
        (s, ex) => ex != null ? $"{s}: {ex.Message}" : s?.ToString(CultureInfo.InvariantCulture) ?? ""
      );

      // Add some nested scopes occasionally
      if (i % 50 == 0) {
        using var nestedScope = _Logger.BeginScope(new Dictionary<string, object> {
          { "NestedOperation", "DetailedProcessing" },
          { "ParentBatch", batchId }
        });

        _Logger.LogDebug("Detailed processing for batch {BatchId}", batchId);
      }
    }
  }

  /// <summary>
  /// Calculates the depth of a scope in the hierarchy.
  /// </summary>
  /// <param name="scope">The scope to analyze.</param>
  /// <param name="allEntries">All log entries for reference.</param>
  /// <returns>The depth of the scope (1 for root, 2+ for nested).</returns>
  private static int CalculateScopeDepth(LogEntry scope, IReadOnlyList<LogEntry> allEntries) {
    var depth = 1;
    var currentParentId = scope.ParentScopeId;

    while (currentParentId != null) {
      var parentScope = allEntries
        .ScopeStartsOnly()
        .FirstOrDefault(s => s.ScopeId == currentParentId);

      if (parentScope == null) {
        break;
      }

      depth++;
      currentParentId = parentScope.ParentScopeId;
    }

    return depth;
  }
}
