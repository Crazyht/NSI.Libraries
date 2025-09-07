using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NSI.Testing.Loggers;
using NSI.Testing.Tests.TestUtilities;

namespace NSI.Testing.Tests.Unit.Loggers;
/// <summary>
/// Tests for the <see cref="LogEntryEnumerableExtensions"/> LINQ extension methods.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the correct filtering behavior of all LINQ extension methods
/// and their integration with standard LINQ operations. They cover type filters,
/// message filters, scope filters, and complex query scenarios.
/// </para>
/// </remarks>
public partial class LogEntryEnumerableExtensionsTests {

  private readonly List<LogEntry> _TestEntries;

  public LogEntryEnumerableExtensionsTests() => _TestEntries = CreateTestDataSet();

  private static List<LogEntry> CreateTestDataSet() {
    var entries = new List<LogEntry>();

    // Add various log levels
    entries.AddRange(LogEntryFactory.CreateVariousLogLevels());

    // Add scope lifecycle
    entries.AddRange(LogEntryFactory.CreateScopeLifecycle(
      variables: new Dictionary<string, object> { { "UserId", 42 }, { "Operation", "ProcessOrder" } }));

    // Add nested scopes
    entries.AddRange(LogEntryFactory.CreateNestedScopes(depth: 2));

    // Add some specific test entries
    entries.Add(LogEntryFactory.CreateLogEntry(LogLevel.Error, "Database connection failed"));
    entries.Add(LogEntryFactory.CreateLogEntry(LogLevel.Warning, "Cache miss for key: user-42"));
    entries.Add(LogEntryFactory.CreateLogEntry(LogLevel.Information, "Processing started"));
    entries.Add(LogEntryFactory.CreateLogEntry(LogLevel.Information, "Processing completed"));

    return entries;
  }

  #region Type Filter Tests

  [Fact]
  public void LogsOnly_WithMixedEntries_ShouldReturnOnlyLogEntries() {
    // Execute filter
    var result = _TestEntries.LogsOnly().ToList();

    // Verify only log entries are returned
    Assert.All(result, entry => Assert.Equal(EntryType.Log, entry.Type));
    Assert.True(result.Count > 0);
    Assert.True(result.Count < _TestEntries.Count); // Should filter out scope entries
  }

  [Fact]
  public void ScopeStartsOnly_WithMixedEntries_ShouldReturnOnlyScopeStartEntries() {
    // Execute filter
    var result = _TestEntries.ScopeStartsOnly().ToList();

    // Verify only scope start entries are returned
    Assert.All(result, entry => Assert.Equal(EntryType.ScopeStart, entry.Type));
    Assert.True(result.Count > 0);
    Assert.True(result.Count < _TestEntries.Count);
  }

  [Fact]
  public void ScopeEndsOnly_WithMixedEntries_ShouldReturnOnlyScopeEndEntries() {
    // Execute filter
    var result = _TestEntries.ScopeEndsOnly().ToList();

    // Verify only scope end entries are returned
    Assert.All(result, entry => Assert.Equal(EntryType.ScopeEnd, entry.Type));
    Assert.True(result.Count > 0);
    Assert.True(result.Count < _TestEntries.Count);
  }

  [Fact]
  public void TypeFilters_WithEmptySequence_ShouldReturnEmpty() {
    // Setup empty sequence
    var emptyEntries = new List<LogEntry>();

    // Test all type filters
    Assert.Empty(emptyEntries.LogsOnly());
    Assert.Empty(emptyEntries.ScopeStartsOnly());
    Assert.Empty(emptyEntries.ScopeEndsOnly());
  }

  #endregion

  #region Level Filter Tests

  [Fact]
  public void WithLogLevel_WithSpecificLevel_ShouldReturnMatchingEntries() {
    // Execute filter for different levels
    var errors = _TestEntries.WithLogLevel(LogLevel.Error).ToList();
    var warnings = _TestEntries.WithLogLevel(LogLevel.Warning).ToList();
    var information = _TestEntries.WithLogLevel(LogLevel.Information).ToList();

    // Verify correct filtering
    Assert.All(errors, entry => Assert.Equal(LogLevel.Error, entry.Level));
    Assert.All(warnings, entry => Assert.Equal(LogLevel.Warning, entry.Level));
    Assert.All(information, entry => Assert.Equal(LogLevel.Information, entry.Level));

    Assert.True(errors.Count > 0);
    Assert.True(warnings.Count > 0);
    Assert.True(information.Count > 0);
  }

  [Fact]
  public void WithLogLevel_WithNonExistentLevel_ShouldReturnEmpty() {
    // Execute filter with level that doesn't exist in test data
    var result = _TestEntries.WithLogLevel(LogLevel.None).ToList();

    // Verify empty result
    Assert.Empty(result);
  }

  [Fact]
  public void WithLogLevel_OnScopeEntries_ShouldReturnEmpty() {
    // Execute filter on scope entries (which have null Level)
    var scopeEntries = _TestEntries.Where(e => e.Type != EntryType.Log);
    var result = scopeEntries.WithLogLevel(LogLevel.Information).ToList();

    // Verify empty result since scope entries have null Level
    Assert.Empty(result);
  }

  #endregion

  #region Message Filter Tests

  [Fact]
  public void WithMessageContains_WithExistingText_ShouldReturnMatchingEntries() {
    // Execute filter
    var result = _TestEntries.WithMessageContains("Processing").ToList();

    // Verify correct filtering
    Assert.All(result, entry => Assert.Contains("Processing", entry.Message ?? "", StringComparison.Ordinal));
    Assert.True(result.Count >= 2); // Should find "Processing started" and "Processing completed"
  }

  [Fact]
  public void WithMessageContains_WithCaseSensitivity_ShouldRespectComparison() {
    // Test case-insensitive (default)
    var caseInsensitive = _TestEntries.WithMessageContains("DATABASE").ToList();

    // Test case-sensitive
    var caseSensitive = _TestEntries.WithMessageContains("DATABASE", StringComparison.Ordinal).ToList();

    // Verify case handling
    Assert.True(caseInsensitive.Count > 0); // Should find "Database" with case-insensitive
    Assert.Empty(caseSensitive); // Should not find "DATABASE" with case-sensitive
  }

  [Fact]
  public void WithMessageStartsWith_WithExistingPrefix_ShouldReturnMatchingEntries() {
    // Execute filter
    var result = _TestEntries.WithMessageStartsWith("Processing").ToList();

    // Verify correct filtering
    Assert.All(result, entry => Assert.StartsWith("Processing", entry.Message ?? "", StringComparison.Ordinal));
    Assert.True(result.Count >= 2);
  }

  [Fact]
  public void WithMessageEndsWith_WithExistingSuffix_ShouldReturnMatchingEntries() {
    // Execute filter
    var result = _TestEntries.WithMessageEndsWith("failed").ToList();

    // Verify correct filtering
    Assert.All(result, entry => Assert.EndsWith("failed", entry.Message ?? "", StringComparison.Ordinal));
    Assert.True(result.Count > 0);
  }

  [Fact]
  public void WithMessageMatch_WithRegexPattern_ShouldReturnMatchingEntries() {
    // Setup regex pattern
    var regex = UserIdRegex();

    // Execute filter
    var result = _TestEntries.WithMessageMatch(regex).ToList();

    // Verify regex matching
    Assert.All(result, entry => Assert.Matches(regex, entry.Message ?? ""));
    Assert.True(result.Count > 0); // Should find "user-42"
  }

  [Fact]
  public void WithMessageMatch_WithNullRegex_ShouldThrowArgumentNullException() {
    // Execute and verify exception
    var exception = Assert.Throws<ArgumentNullException>(() =>
      _TestEntries.WithMessageMatch(null!).ToList());

    Assert.Equal("regex", exception.ParamName);
  }

  [Fact]
  public void MessageFilters_WithNullMessages_ShouldHandleGracefully() {
    // Create entries with null messages (scope entries)
    var entriesWithNulls = new List<LogEntry> {
      LogEntryFactory.CreateScopeStart(),
      LogEntryFactory.CreateLogEntry(LogLevel.Information, "Valid message"),
      LogEntryFactory.CreateScopeEnd(Guid.NewGuid())
    };

    // Test message filters handle null gracefully
    var containsResult = entriesWithNulls.WithMessageContains("Valid").ToList();
    var startsWithResult = entriesWithNulls.WithMessageStartsWith("Valid").ToList();
    var endsWithResult = entriesWithNulls.WithMessageEndsWith("message").ToList();

    // Verify null handling
    Assert.Single(containsResult);
    Assert.Single(startsWithResult);
    Assert.Single(endsWithResult);
  }

  #endregion

  #region Scope Filter Tests

  [Fact]
  public void WithScopeContainingVar_WithExistingKeyValue_ShouldReturnMatchingEntries() {
    // Execute filter
    var result = _TestEntries.WithScopeContainingVar("UserId", 42).ToList();

    // Verify correct filtering
    Assert.All(result, entry => {
      var kvPairs = entry.State.OfType<KeyValuePair<string, object>>();
      Assert.Contains(kvPairs, kv => kv.Key == "UserId" && Equals(kv.Value, 42));
    });
    Assert.True(result.Count > 0);
  }

  [Fact]
  public void WithScopeContainingKey_WithExistingKey_ShouldReturnMatchingEntries() {
    // Execute filter
    var result = _TestEntries.WithScopeContainingKey("Operation").ToList();

    // Verify correct filtering
    Assert.All(result, entry => {
      var kvPairs = entry.State.OfType<KeyValuePair<string, object>>();
      Assert.Contains(kvPairs, kv => kv.Key == "Operation");
    });
    Assert.True(result.Count > 0);
  }

  [Fact]
  public void WithScopeContainingVar_WithNonExistentKeyValue_ShouldReturnEmpty() {
    // Execute filter with non-existent key-value
    var result = _TestEntries.WithScopeContainingVar("NonExistentKey", "value").ToList();

    // Verify empty result
    Assert.Empty(result);
  }

  [Fact]
  public void ScopeFilters_WithNullParameters_ShouldThrowArgumentNullException() {
    // Test null key parameter
    var keyException = Assert.Throws<ArgumentNullException>(() =>
      _TestEntries.WithScopeContainingKey(null!).ToList());
    Assert.Equal("key", keyException.ParamName);

    // Test null key in key-value filter
    var keyValueException = Assert.Throws<ArgumentNullException>(() =>
      _TestEntries.WithScopeContainingVar(null!, "value").ToList());
    Assert.Equal("key", keyValueException.ParamName);
  }

  #endregion

  #region Context Filter Tests

  [Fact]
  public void WithinScope_WithValidScopeId_ShouldReturnEntriesInScope() {
    // Get a scope ID from test data
    var scopeStart = _TestEntries.ScopeStartsOnly().First();
    var scopeId = scopeStart.ScopeId!.Value;

    // Execute filter
    var result = _TestEntries.WithinScope(scopeId).ToList();

    // Verify all entries belong to the scope
    Assert.All(result, entry => Assert.Equal(scopeId, entry.ScopeId));
    Assert.True(result.Count > 0);
  }

  [Fact]
  public void WithParentScope_WithValidParentId_ShouldReturnEntriesWithParent() {
    // Get a parent scope ID from nested test data
    var nestedEntries = _TestEntries.Where(e => e.ParentScopeId.HasValue).ToList();

    if (nestedEntries.Count > 0) {
      var parentId = nestedEntries[0].ParentScopeId!.Value;

      // Execute filter
      var result = _TestEntries.WithParentScope(parentId).ToList();

      // Verify all entries have the correct parent
      Assert.All(result, entry => Assert.Equal(parentId, entry.ParentScopeId));
      Assert.True(result.Count > 0);
    }
  }

  [Fact]
  public void WithoutScope_ShouldReturnEntriesWithoutScope() {
    // Execute filter
    var result = _TestEntries.WithoutScope().ToList();

    // Verify all entries have no scope
    Assert.All(result, entry => Assert.Null(entry.ScopeId));
    Assert.True(result.Count > 0);
  }

  [Fact]
  public void WithinScope_WithNonExistentScopeId_ShouldReturnEmpty() {
    // Execute filter with non-existent scope ID
    var result = _TestEntries.WithinScope(Guid.NewGuid()).ToList();

    // Verify empty result
    Assert.Empty(result);
  }

  #endregion

  #region Event and Exception Filter Tests

  [Fact]
  public void WithEventId_WithEventIdStruct_ShouldReturnMatchingEntries() {
    // Create test entries with specific event IDs
    var testEventId = new EventId(123, "TestEvent");
    var entries = new List<LogEntry> {
      LogEntryFactory.CreateLogEntry(eventId: testEventId),
      LogEntryFactory.CreateLogEntry(eventId: new EventId(456, "OtherEvent")),
      LogEntryFactory.CreateLogEntry(eventId: testEventId)
    };

    // Execute filter
    var result = entries.WithEventId(testEventId).ToList();

    // Verify correct filtering
    Assert.Equal(2, result.Count);
    Assert.All(result, entry => Assert.Equal(testEventId, entry.EventId));
  }

  [Fact]
  public void WithEventId_WithIntegerEventId_ShouldReturnMatchingEntries() {
    // Create test entries with specific event IDs
    var entries = new List<LogEntry> {
      LogEntryFactory.CreateLogEntry(eventId: new EventId(123, "TestEvent")),
      LogEntryFactory.CreateLogEntry(eventId: new EventId(456, "OtherEvent")),
      LogEntryFactory.CreateLogEntry(eventId: new EventId(123, "AnotherTestEvent"))
    };

    // Execute filter
    var result = entries.WithEventId(123).ToList();

    // Verify correct filtering
    Assert.Equal(2, result.Count);
    Assert.All(result, entry => Assert.Equal(123, entry.EventId?.Id));
  }

  [Fact]
  public void WithException_ShouldReturnEntriesWithExceptions() {
    // Execute filter
    var result = _TestEntries.WithException().ToList();

    // Verify all entries have exceptions
    Assert.All(result, entry => Assert.NotNull(entry.Exception));
    Assert.True(result.Count > 0);
  }

  [Fact]
  public void WithException_Generic_ShouldReturnEntriesWithSpecificExceptionType() {
    // Create test entries with different exception types
    var entries = new List<LogEntry> {
      LogEntryFactory.CreateLogEntry(exception: new InvalidOperationException("Invalid op")),
      LogEntryFactory.CreateLogEntry(exception: new ArgumentException("Bad arg")),
      LogEntryFactory.CreateLogEntry(exception: new InvalidOperationException("Another invalid op")),
      LogEntryFactory.CreateLogEntry() // No exception
    };

    // Execute filter
    var result = entries.WithException<InvalidOperationException>().ToList();

    // Verify correct exception type filtering
    Assert.Equal(2, result.Count);
    Assert.All(result, entry => Assert.IsType<InvalidOperationException>(entry.Exception));
  }

  [Fact]
  public void WithException_Generic_WithInheritance_ShouldReturnDerivedTypes() {
    // Create test entries with exception inheritance
    var entries = new List<LogEntry> {
      LogEntryFactory.CreateLogEntry(exception: new ArgumentException("Arg exception")),
      LogEntryFactory.CreateLogEntry(exception: new ArgumentNullException("Null arg")), // Derives from ArgumentException
      LogEntryFactory.CreateLogEntry(exception: new InvalidOperationException("Invalid op"))
    };

    // Execute filter
    var result = entries.WithException<ArgumentException>().ToList();

    // Verify inheritance handling
    Assert.Equal(2, result.Count); // Should include both ArgumentException and ArgumentNullException
    Assert.All(result, entry => Assert.True(entry.Exception is ArgumentException));
  }

  #endregion

  #region LINQ Integration Tests

  [Fact]
  public void LinqIntegration_ChainedFilters_ShouldWorkCorrectly() {
    // Execute chained LINQ operations
    var result = _TestEntries
      .LogsOnly()
      .WithLogLevel(LogLevel.Information)
      .WithMessageContains("Processing")
      .OrderBy(e => e.Message)
      .ToList();

    // Verify chained filtering
    Assert.All(result, entry => {
      Assert.Equal(EntryType.Log, entry.Type);
      Assert.Equal(LogLevel.Information, entry.Level);
      Assert.Contains("Processing", entry.Message ?? "", StringComparison.Ordinal);
    });

    // Verify ordering
    for (var i = 1; i < result.Count; i++) {
      Assert.True(string.Compare(result[i - 1].Message, result[i].Message, StringComparison.Ordinal) <= 0);
    }
  }

  [Fact]
  public void LinqIntegration_GroupBy_ShouldWorkWithExtensions() {
    // Execute grouping with extensions
    var result = _TestEntries
      .LogsOnly()
      .GroupBy(e => e.Level)
      .Select(g => new { Level = g.Key, Count = g.Count(), Messages = g.Select(e => e.Message).ToList() })
      .OrderBy(x => x.Level)
      .ToList();

    // Verify grouping works correctly
    Assert.True(result.Count > 0);
    Assert.All(result, group => {
      Assert.True(group.Count > 0);
      Assert.NotNull(group.Level);
    });
  }

  [Fact]
  public void LinqIntegration_SelectMany_WithScopeHierarchy_ShouldWorkCorrectly() {
    // Execute complex LINQ with scope hierarchy
    var result = _TestEntries
      .ScopeStartsOnly()
      .WithScopeContainingKey("Level")
      .SelectMany(parentScope =>
        _TestEntries.WithParentScope(parentScope.ScopeId!.Value))
      .LogsOnly()
      .ToList();

    // Verify SelectMany integration
    Assert.All(result, entry => {
      Assert.Equal(EntryType.Log, entry.Type);
      Assert.NotNull(entry.ParentScopeId);
    });
  }

  [Fact]
  public void LinqIntegration_ComplexQuery_ShouldHandleMultipleOperations() {
    // Execute complex query combining multiple LINQ operations
    var result = _TestEntries
      .Where(e => e.Type == EntryType.Log)
      .WithLogLevel(LogLevel.Error)
      .Union(_TestEntries.WithLogLevel(LogLevel.Critical))
      .Concat(_TestEntries.WithLogLevel(LogLevel.Warning).Take(1))
      .Distinct()
      .OrderByDescending(e => e.Level)
      .ThenBy(e => e.Message)
      .ToList();

    // Verify complex query execution
    Assert.True(result.Count > 0);
    Assert.All(result, entry => Assert.True(entry.Level == LogLevel.Error ||
                 entry.Level == LogLevel.Critical ||
                 entry.Level == LogLevel.Warning));
  }

  #endregion

  #region Edge Cases and Error Handling

  [Fact]
  public void Extensions_WithNullSource_ShouldThrowArgumentNullException() {
    // Test all extensions with null source
    IEnumerable<LogEntry> nullSource = null!;

    // Type filters
    Assert.Throws<ArgumentNullException>(() => nullSource.LogsOnly().ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.ScopeStartsOnly().ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.ScopeEndsOnly().ToList());

    // Level filters
    Assert.Throws<ArgumentNullException>(() => nullSource.WithLogLevel(LogLevel.Information).ToList());

    // Message filters
    Assert.Throws<ArgumentNullException>(() => nullSource.WithMessageContains("test").ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.WithMessageStartsWith("test").ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.WithMessageEndsWith("test").ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.WithMessageMatch(TestRegex()).ToList());

    // Context filters
    Assert.Throws<ArgumentNullException>(() => nullSource.WithinScope(Guid.NewGuid()).ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.WithParentScope(Guid.NewGuid()).ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.WithoutScope().ToList());

    // Exception filters
    Assert.Throws<ArgumentNullException>(() => nullSource.WithException().ToList());
    Assert.Throws<ArgumentNullException>(() => nullSource.WithException<Exception>().ToList());
  }

  [Fact]
  public void Extensions_WithEmptySource_ShouldReturnEmpty() {
    // Setup empty source
    var emptySource = new List<LogEntry>();

    // Test all extensions return empty for empty source
    Assert.Empty(emptySource.LogsOnly());
    Assert.Empty(emptySource.ScopeStartsOnly());
    Assert.Empty(emptySource.ScopeEndsOnly());
    Assert.Empty(emptySource.WithLogLevel(LogLevel.Information));
    Assert.Empty(emptySource.WithMessageContains("test"));
    Assert.Empty(emptySource.WithMessageStartsWith("test"));
    Assert.Empty(emptySource.WithMessageEndsWith("test"));
    Assert.Empty(emptySource.WithMessageMatch(TestRegex()));
    Assert.Empty(emptySource.WithScopeContainingVar("key", "value"));
    Assert.Empty(emptySource.WithScopeContainingKey("key"));
    Assert.Empty(emptySource.WithinScope(Guid.NewGuid()));
    Assert.Empty(emptySource.WithParentScope(Guid.NewGuid()));
    Assert.Empty(emptySource.WithoutScope());
    Assert.Empty(emptySource.WithEventId(new EventId(1)));
    Assert.Empty(emptySource.WithEventId(1));
    Assert.Empty(emptySource.WithException());
    Assert.Empty(emptySource.WithException<Exception>());
  }

  [Fact]
  public void Extensions_LazyEvaluation_ShouldNotExecuteUntilEnumerated() {
    // Setup source that will throw when enumerated
    var throwingSource = new ThrowingEnumerable<LogEntry>();

    // These should not throw (lazy evaluation)
    var query1 = throwingSource.LogsOnly();
    var query2 = throwingSource.WithLogLevel(LogLevel.Information);
    var query3 = throwingSource.WithMessageContains("test");

    // Verify no exceptions thrown yet
    Assert.NotNull(query1);
    Assert.NotNull(query2);
    Assert.NotNull(query3);

    // These should throw when enumerated
    Assert.Throws<InvalidOperationException>(() => query1.ToList());
    Assert.Throws<InvalidOperationException>(() => query2.ToList());
    Assert.Throws<InvalidOperationException>(() => query3.ToList());
  }

  [Fact]
  public void Extensions_Performance_WithLargeDataSet_ShouldHandleEfficiently() {
    // Setup large dataset
    var largeDataSet = new List<LogEntry>();
    for (var i = 0; i < 10000; i++) {
      largeDataSet.Add(LogEntryFactory.CreateLogEntry(
        level: (LogLevel)((i % 6) + 1), // Cycle through log levels
        message: $"Message {i}"));
    }

    // Execute performance test
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    var result = largeDataSet
      .LogsOnly()
      .WithLogLevel(LogLevel.Error)
      .WithMessageContains("Message")
      .Take(100)
      .ToList();

    stopwatch.Stop();

    // Verify performance and correctness
    Assert.True(result.Count > 0);
    Assert.True(stopwatch.ElapsedMilliseconds < 100,
      $"Large dataset query took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
  }

  #endregion

  #region Real-World Scenario Tests

  [Fact]
  public void RealWorldScenario_ErrorAnalysis_ShouldFindCriticalIssues() {
    // Setup realistic scenario data
    var store = new InMemoryLogEntryStore();
    var logger = new MockLogger<object>(store);

    // Simulate application execution with various scenarios
    using (var requestScope = logger.BeginScope(new Dictionary<string, object> {
      { "RequestId", "REQ-001" },
      { "UserId", 12345 }
    })) {
      logger.Log(LogLevel.Information, new EventId(1), "Request started", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      using (var dbScope = logger.BeginScope(new Dictionary<string, object> {
        { "Operation", "DatabaseQuery" },
        { "Table", "Users" }
      })) {
        logger.Log(LogLevel.Warning, new EventId(2), "Query took longer than expected: 2500ms", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
        logger.Log(LogLevel.Error, new EventId(3), "Database connection timeout",
          new TimeoutException("Connection timeout"), (s, ex) => $"{s}: {ex?.Message}");
      }

      logger.Log(LogLevel.Critical, new EventId(4), "Request failed due to database issues", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    // Execute real-world analysis query
    var criticalIssues = store.GetAll()
      .LogsOnly()
      .Where(e => e.Level == LogLevel.Error || e.Level == LogLevel.Critical)
      .Where(e => e.ScopeId.HasValue)
      .GroupBy(e => e.ScopeId)
      .Select(g => new {
        ScopeId = g.Key,
        ErrorCount = g.Count(),
        HasException = g.Any(e => e.Exception != null),
        Messages = g.Select(e => e.Message).ToList()
      })
      .Where(x => x.ErrorCount > 1 || x.HasException)
      .ToList();

    // Verify analysis results
    Assert.True(criticalIssues.Count > 0);
    Assert.Contains(criticalIssues, issue => issue.HasException);
    Assert.Contains(criticalIssues, issue => issue.ErrorCount > 0);
  }

  [Fact]
  public void RealWorldScenario_ScopeHierarchyAnalysis_ShouldTrackUserJourney() {
    // Setup user journey simulation
    var store = new InMemoryLogEntryStore();
    var logger = new MockLogger<object>(store);

    using (var sessionScope = logger.BeginScope(new Dictionary<string, object> {
      { "SessionId", "SESS-123" },
      { "UserId", 42 }
    })) {
      logger.Log(LogLevel.Information, new EventId(1), "User session started", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

      using (var pageScope = logger.BeginScope(new Dictionary<string, object> {
        { "Page", "ProductCatalog" },
        { "Action", "Browse" }
      })) {
        logger.Log(LogLevel.Debug, new EventId(2), "Loading product catalog", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
        logger.Log(LogLevel.Information, new EventId(3), "Displayed 50 products", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");

        using var actionScope = logger.BeginScope(new Dictionary<string, object> {
          { "ProductId", "PROD-789" },
          { "Action", "AddToCart" }
        });
        logger.Log(LogLevel.Information, new EventId(4), "Product added to cart", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      }

      using var checkoutScope = logger.BeginScope(new Dictionary<string, object> {
        { "Page", "Checkout" },
        { "Step", "Payment" }
      });
      logger.Log(LogLevel.Information, new EventId(5), "Payment processing started", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
      logger.Log(LogLevel.Information, new EventId(6), "Payment completed successfully", null, (s, _) => s?.ToString(CultureInfo.InvariantCulture) ?? "");
    }

    // Execute user journey analysis - CORRECTION ICI
    var userSessionScope = store.GetAll()
      .ScopeStartsOnly()
      .WithScopeContainingVar("UserId", 42)
      .FirstOrDefault();

    Assert.NotNull(userSessionScope);

    // Récupérer tous les logs dans la hiérarchie complète du scope utilisateur
    var userJourney = store.GetAll()
      .LogsOnly()
      .Where(log => log.ScopeId.HasValue && IsWithinUserSession(store, log.ScopeId.Value, userSessionScope.ScopeId!.Value))
      .OrderBy(e => e.EventId?.Id ?? 0)
      .Select(e => e.Message)
      .ToList();

    // Verify journey tracking
    Assert.True(userJourney.Count >= 5);
    Assert.Contains("User session started", userJourney);
    Assert.Contains("Product added to cart", userJourney);
    Assert.Contains("Payment completed successfully", userJourney);
  }

  /// <summary>
  /// Helper method to check if a scope is within the user session hierarchy.
  /// </summary>
  private static bool IsWithinUserSession(InMemoryLogEntryStore store, Guid scopeId, Guid userSessionScopeId) {
    if (scopeId == userSessionScopeId) {
      return true;
    }

    // Rechercher le scope dans la hiérarchie
    var allScopes = store.GetAll().Where(e => e.Type == EntryType.ScopeStart).ToList();
    var currentScope = allScopes.Find(s => s.ScopeId == scopeId);

    while (currentScope != null && currentScope.ParentScopeId.HasValue) {
      if (currentScope.ParentScopeId == userSessionScopeId) {
        return true;
      }
      currentScope = allScopes.Find(s => s.ScopeId == currentScope.ParentScopeId);
    }

    return false;
  }

  #endregion

  /// <summary>
  /// Helper class for testing lazy evaluation behavior.
  /// </summary>
  private sealed class ThrowingEnumerable<T>: IEnumerable<T> {
    public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("This enumerable always throws when enumerated");

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
  }

  [GeneratedRegex(@"user-\d+", RegexOptions.IgnoreCase, "en-DE")]
  private static partial Regex UserIdRegex();
  [GeneratedRegex("test")]
  private static partial Regex TestRegex();
}
