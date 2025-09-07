using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using NSI.Testing.Loggers;

namespace NSI.Testing.Tests.Unit.Loggers {
  /// <summary>
  /// Tests for the <see cref="MockLogger{T}"/> functionality and scope management.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests verify the correct implementation of ILogger interface,
  /// scope management with hierarchy tracking, and AsyncLocal context preservation.
  /// They cover both single-threaded and multi-threaded scenarios.
  /// </para>
  /// </remarks>
  public class MockLoggerTests {

    /// <summary>
    /// Simple test service class for logger testing.
    /// </summary>
    [SuppressMessage(
      "Minor Code Smell",
      "S2094:Classes should not be empty",
      Justification = "Empty test class for generic type parameter")]
    private sealed class TestService {
      // Empty test class for generic type parameter
    }

    [Fact]
    public void Constructor_WithValidStore_ShouldInitializeCorrectly() {
      // Setup store
      var store = new InMemoryLogEntryStore();

      // Execute constructor
      var logger = new MockLogger<TestService>(store);

      // Verify logger is created without exceptions
      Assert.NotNull(logger);
      Assert.True(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void Constructor_WithNullStore_ShouldThrowArgumentNullException() {
      // Execute and verify exception
      var exception = Assert.Throws<ArgumentNullException>(() => new MockLogger<TestService>(null!));
      Assert.Equal("store", exception.ParamName);
    }

    [Fact]
    public void IsEnabled_WithAnyLogLevel_ShouldReturnTrue() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      // Test all log levels
      Assert.True(logger.IsEnabled(LogLevel.Trace));
      Assert.True(logger.IsEnabled(LogLevel.Debug));
      Assert.True(logger.IsEnabled(LogLevel.Information));
      Assert.True(logger.IsEnabled(LogLevel.Warning));
      Assert.True(logger.IsEnabled(LogLevel.Error));
      Assert.True(logger.IsEnabled(LogLevel.Critical));
      Assert.True(logger.IsEnabled(LogLevel.None));
    }

    [Fact]
    public void Log_WithValidParameters_ShouldStoreLogEntry() {
      // Setup logger and parameters
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      var logLevel = LogLevel.Information;
      var eventId = new EventId(123, "TestEvent");
      var state = "Test log message";
      var exception = new InvalidOperationException("Test exception");

      // Execute log operation
      logger.Log(logLevel, eventId, state, exception, (s, _) => $"Formatted: {s}");

      // Verify log entry is stored
      var entries = store.GetAll();
      Assert.Single(entries);

      var entry = entries[0];
      Assert.Equal(EntryType.Log, entry.Type);
      Assert.Equal(logLevel, entry.Level);
      Assert.Equal(eventId, entry.EventId);
      Assert.Equal("Formatted: Test log message", entry.Message);
      Assert.Same(exception, entry.Exception);
      Assert.Equal([state], entry.State);
      Assert.Null(entry.ScopeId);
      Assert.Null(entry.ParentScopeId);
    }

    [Fact]
    public void Log_WithNullFormatter_ShouldThrowArgumentNullException() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      // Execute and verify exception
      var exception = Assert.Throws<ArgumentNullException>(() =>
        logger.Log(LogLevel.Information, new EventId(1), "state", null, null!));

      Assert.Equal("formatter", exception.ParamName);
    }

    [Fact]
    public void Log_WithArrayState_ShouldPreserveStateArray() {
      // Setup logger with array state
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      var stateArray = new object[] { "item1", "item2", 42 };

      // Execute log operation
      logger.Log(LogLevel.Debug, new EventId(1), stateArray, null, (_, _) => "Array message");

      // Verify state array is preserved
      var entries = store.GetAll();
      Assert.Single(entries);
      Assert.Equal(stateArray, entries[0].State);
    }

    [Fact]
    public void BeginScope_WithValidState_ShouldCreateScopeStartEntry() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      var scopeState = "Test scope";

      // Execute scope creation
      using var scope = logger.BeginScope(scopeState);

      // Verify scope start entry is created
      var entries = store.GetAll();
      Assert.Single(entries);

      var entry = entries[0];
      Assert.Equal(EntryType.ScopeStart, entry.Type);
      Assert.NotEqual(Guid.Empty, entry.ScopeId);
      Assert.Null(entry.ParentScopeId);
      Assert.Null(entry.Level);
      Assert.Null(entry.EventId);
      Assert.Null(entry.Message);
      Assert.Null(entry.Exception);
      Assert.Equal([scopeState], entry.State);
    }

    [Fact]
    [SuppressMessage(
      "Performance",
      "CA1848:Use the LoggerMessage delegates",
      Justification = "We also need to test not performance method.")]
    [SuppressMessage("Performance",
      "CA2254: The logging message template should not vary between calls to 'LoggerExtensions.BeginScope(ILogger, string, params object?[])'",
      Justification = "Test case requires dynamic scope state.")]
    public void BeginScope_WithNullState_ShouldThrowArgumentNullException() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      // Execute and verify exception
      var exception = Assert.Throws<ArgumentNullException>(() => logger.BeginScope<object>(null!));
      Assert.Equal("state", exception.ParamName);
    }

    [Fact]
    public void BeginScope_WithKeyValuePairs_ShouldStoreAsStateArray() {
      // Setup logger with key-value pairs
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      var kvPairs = new Dictionary<string, object> {
        { "UserId", 42 },
        { "Operation", "ProcessOrder" }
      };

      // Execute scope creation
      using var scope = logger.BeginScope(kvPairs);

      // Verify key-value pairs are stored correctly
      var entries = store.GetAll();
      Assert.Single(entries);

      var entry = entries[0];
      Assert.Equal(EntryType.ScopeStart, entry.Type);
      Assert.Equal(2, entry.State.Length);

      // Verify state contains KeyValuePair objects
      Assert.All(entry.State, item => Assert.IsType<KeyValuePair<string, object>>(item));
    }

    [Fact]
    public void ScopeDisposal_ShouldCreateScopeEndEntry() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      var scopeState = "Test scope";
      Guid scopeId;

      // Execute scope lifecycle
      using (var scope = logger.BeginScope(scopeState)) {
        var startEntries = store.GetAll();
        scopeId = startEntries[0].ScopeId!.Value;
      } // Scope disposed here

      // Verify scope end entry is created
      var allEntries = store.GetAll();
      Assert.Equal(2, allEntries.Count);

      var endEntry = allEntries[1];
      Assert.Equal(EntryType.ScopeEnd, endEntry.Type);
      Assert.Equal(scopeId, endEntry.ScopeId);
      Assert.Null(endEntry.ParentScopeId);
      Assert.Empty(endEntry.State);
    }

    [Fact]
    public void Log_WithinScope_ShouldIncludeScopeInformation() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      var scopeState = "Test scope";

      // Execute logging within scope
      using var scope = logger.BeginScope(scopeState);
      logger.Log(LogLevel.Information, new EventId(1), "Test message", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      // Get scope ID from scope start entry
      var scopeStartEntry = store.GetAll().First(e => e.Type == EntryType.ScopeStart);
      var scopeId = scopeStartEntry.ScopeId!.Value;

      // Verify log entry includes scope information
      var logEntry = store.GetAll().First(e => e.Type == EntryType.Log);
      Assert.Equal(scopeId, logEntry.ScopeId);
      Assert.Null(logEntry.ParentScopeId); // No parent for first-level scope
    }

    [Fact]
    public void NestedScopes_ShouldMaintainHierarchy() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      Guid outerScopeId;
      Guid innerScopeId;

      // Execute nested scopes
      using (var outerScope = logger.BeginScope("Outer scope")) {
        outerScopeId = store.GetAll()[store.GetAll().Count - 1].ScopeId!.Value;

        using (var innerScope = logger.BeginScope("Inner scope")) {
          innerScopeId = store.GetAll()[store.GetAll().Count - 1].ScopeId!.Value;

          // Log within inner scope
          logger.Log(LogLevel.Information, new EventId(1), "Inner message", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
        }

        // Log within outer scope after inner scope ends
        logger.Log(LogLevel.Warning, new EventId(2), "Outer message", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      }

      // Verify hierarchy is maintained
      var entries = store.GetAll();
      Assert.Equal(6, entries.Count); // 2 starts + 2 logs + 2 ends

      // Verify inner scope has outer scope as parent
      var innerScopeStart = entries.First(e => e.Type == EntryType.ScopeStart && e.ScopeId == innerScopeId);
      Assert.Equal(outerScopeId, innerScopeStart.ParentScopeId);

      // Verify log in inner scope references both scopes correctly
      var innerLog = entries.First(e => e.Type == EntryType.Log && e.Message == "Inner message");
      Assert.Equal(innerScopeId, innerLog.ScopeId);
      Assert.Equal(outerScopeId, innerLog.ParentScopeId);

      // Verify log in outer scope only references outer scope
      var outerLog = entries.First(e => e.Type == EntryType.Log && e.Message == "Outer message");
      Assert.Equal(outerScopeId, outerLog.ScopeId);
      Assert.Null(outerLog.ParentScopeId);
    }

    [Fact]
    public void MultipleScopes_AtSameLevel_ShouldBeIndependent() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      Guid firstScopeId;
      Guid secondScopeId;

      // Execute sequential scopes at same level
      using (var firstScope = logger.BeginScope("First scope")) {
        firstScopeId = store.GetAll()[store.GetAll().Count - 1].ScopeId!.Value;
        logger.Log(LogLevel.Information, new EventId(1), "First message", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      }

      using (var secondScope = logger.BeginScope("Second scope")) {
        secondScopeId = store.GetAll()[store.GetAll().Count - 1].ScopeId!.Value;
        logger.Log(LogLevel.Information, new EventId(2), "Second message", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      }

      // Verify scopes are independent
      var entries = store.GetAll();
      Assert.Equal(6, entries.Count); // 2 starts + 2 logs + 2 ends

      // Verify different scope IDs
      Assert.NotEqual(firstScopeId, secondScopeId);

      // Verify both scopes have no parent
      var scopeStarts = entries.Where(e => e.Type == EntryType.ScopeStart).ToList();
      Assert.All(scopeStarts, entry => Assert.Null(entry.ParentScopeId));
    }

    [Fact]
    public async Task AsyncLocal_ScopeContext_ShouldPreserveAcrossAsyncCalls() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      // Execute async scope test
      var task = Task.Run(async () => {
        using var scope = logger.BeginScope("Async scope");
        // Log before await
        logger.Log(LogLevel.Information, new EventId(1), "Before await", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

        // Simulate async operation
        await Task.Delay(10);

        // Log after await - scope should still be active
        logger.Log(LogLevel.Information, new EventId(2), "After await", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      });

      await task;

      // Verify scope context was preserved
      var entries = store.GetAll();
      var logEntries = entries.Where(e => e.Type == EntryType.Log).ToList();

      Assert.Equal(2, logEntries.Count);
      Assert.NotNull(logEntries[0].ScopeId);
      Assert.NotNull(logEntries[1].ScopeId);
      Assert.Equal(logEntries[0].ScopeId, logEntries[1].ScopeId); // Same scope for both logs
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentLogging_ShouldHandleCorrectly() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      const int threadCount = 10;
      const int logsPerThread = 50;

      var tasks = new Task[threadCount];

      // Execute concurrent logging
      for (var i = 0; i < threadCount; i++) {
        var threadId = i;
        tasks[i] = Task.Run(() => {
          for (var j = 0; j < logsPerThread; j++) {
            logger.Log(LogLevel.Information, new EventId(j),
              $"Thread {threadId} - Log {j}", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
          }
        });
      }

      await Task.WhenAll(tasks);

      // Verify all logs were recorded
      var entries = store.GetAll();
      Assert.Equal(threadCount * logsPerThread, entries.Count);
      Assert.All(entries, entry => Assert.Equal(EntryType.Log, entry.Type));
    }

    [Fact]
    [SuppressMessage("Major Code Smell", "S2925:\"Thread.Sleep\" should not be used in tests", Justification = "Needed to increase contention on test.")]
    public async Task ThreadSafety_ConcurrentScopes_ShouldMaintainSeparateContexts() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);
      const int threadCount = 5;

      var tasks = new Task[threadCount];

      // Execute concurrent scoped operations
      for (var i = 0; i < threadCount; i++) {
        var threadId = i;
        tasks[i] = Task.Run(() => {
          using var scope = logger.BeginScope($"Thread {threadId} scope");
          logger.Log(LogLevel.Information, new EventId(1),
            $"Thread {threadId} message", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

          Thread.Sleep(10); // Increase contention

          logger.Log(LogLevel.Warning, new EventId(2),
            $"Thread {threadId} warning", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
        });
      }

      await Task.WhenAll(tasks);

      // Verify scope isolation
      var entries = store.GetAll();
      var scopeStarts = entries.Where(e => e.Type == EntryType.ScopeStart).ToList();
      var scopeEnds = entries.Where(e => e.Type == EntryType.ScopeEnd).ToList();
      var logs = entries.Where(e => e.Type == EntryType.Log).ToList();

      Assert.Equal(threadCount, scopeStarts.Count);
      Assert.Equal(threadCount, scopeEnds.Count);
      Assert.Equal(threadCount * 2, logs.Count); // 2 logs per thread

      // Verify each scope has unique ID
      var scopeIds = scopeStarts.Select(e => e.ScopeId!.Value).ToList();
      Assert.Equal(threadCount, scopeIds.Distinct().Count());

      // Verify logs are properly scoped
      Assert.All(logs, log => Assert.NotNull(log.ScopeId));
      Assert.All(logs, log => Assert.Contains(log.ScopeId!.Value, scopeIds));
    }

    [Fact]
    public void ScopeDisposal_MultipleTimes_ShouldNotCreateDuplicateEndEntries() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      var scope = logger.BeginScope("Test scope");

      // Dispose multiple times
      scope.Dispose();
      scope.Dispose();
      scope.Dispose();

      // Verify only one end entry is created
      var entries = store.GetAll();
      var endEntries = entries.Where(e => e.Type == EntryType.ScopeEnd).ToList();
      Assert.Single(endEntries);
    }

    [Fact]
    public void ComplexScenario_MultipleNestedScopesWithLogging_ShouldMaintainCorrectHierarchy() {
      // Setup logger
      var store = new InMemoryLogEntryStore();
      var logger = new MockLogger<TestService>(store);

      // Execute complex nested scenario
      using (var level1 = logger.BeginScope(new Dictionary<string, object> { { "Level", 1 } })) {
        logger.Log(LogLevel.Information, new EventId(1), "Level 1 log", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

        using (var level2a = logger.BeginScope(new Dictionary<string, object> { { "Level", 2 }, { "Branch", "A" } })) {
          logger.Log(LogLevel.Debug, new EventId(2), "Level 2A log", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

          using (var level3 = logger.BeginScope(new Dictionary<string, object> { { "Level", 3 } })) {
            logger.Log(LogLevel.Warning, new EventId(3), "Level 3 log", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
          }

          logger.Log(LogLevel.Error, new EventId(4), "Level 2A log after 3", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
        }

        using (var level2b = logger.BeginScope(new Dictionary<string, object> { { "Level", 2 }, { "Branch", "B" } })) {
          logger.Log(LogLevel.Critical, new EventId(5), "Level 2B log", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
        }

        logger.Log(LogLevel.Trace, new EventId(6), "Level 1 final log", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      }

      // Verify complex hierarchy
      var entries = store.GetAll();
      var scopeStarts = entries.Where(e => e.Type == EntryType.ScopeStart).ToList();
      var logs = entries.Where(e => e.Type == EntryType.Log).ToList();
      var scopeEnds = entries.Where(e => e.Type == EntryType.ScopeEnd).ToList();

      // Should have 4 scope starts, 6 logs, 4 scope ends
      Assert.Equal(4, scopeStarts.Count);
      Assert.Equal(6, logs.Count);
      Assert.Equal(4, scopeEnds.Count);

      // Verify hierarchy relationships
      var level1Id = scopeStarts[0].ScopeId!.Value;
      var level2aId = scopeStarts[1].ScopeId!.Value;

      Assert.Null(scopeStarts[0].ParentScopeId); // Level 1 has no parent
      Assert.Equal(level1Id, scopeStarts[1].ParentScopeId); // Level 2A parent is Level 1
      Assert.Equal(level2aId, scopeStarts[2].ParentScopeId); // Level 3 parent is Level 2A
      Assert.Equal(level1Id, scopeStarts[3].ParentScopeId); // Level 2B parent is Level 1
    }
  }
}
