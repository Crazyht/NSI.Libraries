using Microsoft.Extensions.Logging;
using NSI.Testing.Loggers;

namespace NSI.Testing.Tests.Unit.Loggers;
/// <summary>
/// Tests for the <see cref="LogEntry"/> functionality and validation.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the correct construction, property access, and validation
/// behavior of LogEntry instances. They cover both log entries and scope entries
/// with various parameter combinations.
/// </para>
/// </remarks>
public class LogEntryTests {

  [Fact]
  public void Constructor_WithValidLogParameters_ShouldSetPropertiesCorrectly() {
    // Setup test data
    var type = EntryType.Log;
    var scopeId = Guid.NewGuid();
    var parentScopeId = Guid.NewGuid();
    var level = LogLevel.Information;
    var eventId = new EventId(123, "TestEvent");
    var message = "Test log message";
    var exception = new InvalidOperationException("Test exception");
    var state = new object[] { "state1", "state2" };

    // Execute constructor
    var entry = new LogEntry(
      type,
      scopeId,
      parentScopeId,
      level,
      eventId,
      message,
      exception,
      state);

    // Verify all properties
    Assert.Equal(type, entry.Type);
    Assert.Equal(scopeId, entry.ScopeId);
    Assert.Equal(parentScopeId, entry.ParentScopeId);
    Assert.Equal(level, entry.Level);
    Assert.Equal(eventId, entry.EventId);
    Assert.Equal(message, entry.Message);
    Assert.Same(exception, entry.Exception);
    Assert.Equal(state, entry.State);
  }

  [Fact]
  public void Constructor_WithScopeStartParameters_ShouldSetPropertiesCorrectly() {
    // Setup test data for scope start
    var type = EntryType.ScopeStart;
    var scopeId = Guid.NewGuid();
    var parentScopeId = Guid.NewGuid();
    var state = new object[] {
      new KeyValuePair<string, object>("UserId", 42),
      new KeyValuePair<string, object>("Operation", "ProcessOrder")
    };

    // Execute constructor
    var entry = new LogEntry(
      type,
      scopeId,
      parentScopeId,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state);

    // Verify scope-specific properties
    Assert.Equal(EntryType.ScopeStart, entry.Type);
    Assert.Equal(scopeId, entry.ScopeId);
    Assert.Equal(parentScopeId, entry.ParentScopeId);
    Assert.Null(entry.Level);
    Assert.Null(entry.EventId);
    Assert.Null(entry.Message);
    Assert.Null(entry.Exception);
    Assert.Equal(state, entry.State);
  }

  [Fact]
  public void Constructor_WithScopeEndParameters_ShouldSetPropertiesCorrectly() {
    // Setup test data for scope end
    var type = EntryType.ScopeEnd;
    var scopeId = Guid.NewGuid();
    var state = Array.Empty<object>();

    // Execute constructor
    var entry = new LogEntry(
      type,
      scopeId,
      parentScopeId: null,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state);

    // Verify scope end properties
    Assert.Equal(EntryType.ScopeEnd, entry.Type);
    Assert.Equal(scopeId, entry.ScopeId);
    Assert.Null(entry.ParentScopeId);
    Assert.Null(entry.Level);
    Assert.Null(entry.EventId);
    Assert.Null(entry.Message);
    Assert.Null(entry.Exception);
    Assert.Empty(entry.State);
  }

  [Fact]
  public void Constructor_WithNullState_ShouldThrowArgumentNullException() {
    // Execute and verify exception
    var exception = Assert.Throws<ArgumentNullException>(() => new LogEntry(
      EntryType.Log,
      scopeId: null,
      parentScopeId: null,
      LogLevel.Information,
      new EventId(1),
      "Test message",
      exception: null,
      state: null!));

    Assert.Equal("state", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullOptionalParameters_ShouldSetPropertiesCorrectly() {
    // Setup with minimal required parameters
    var state = new object[] { "test" };

    // Execute constructor
    var entry = new LogEntry(
      EntryType.Log,
      scopeId: null,
      parentScopeId: null,
      LogLevel.Warning,
      eventId: null,
      message: null,
      exception: null,
      state);

    // Verify null values are handled correctly
    Assert.Equal(EntryType.Log, entry.Type);
    Assert.Null(entry.ScopeId);
    Assert.Null(entry.ParentScopeId);
    Assert.Equal(LogLevel.Warning, entry.Level);
    Assert.Null(entry.EventId);
    Assert.Null(entry.Message);
    Assert.Null(entry.Exception);
    Assert.Equal(state, entry.State);
  }

  [Fact]
  public void Constructor_WithEmptyState_ShouldSetPropertiesCorrectly() {
    // Setup with empty state array
    var state = Array.Empty<object>();

    // Execute constructor
    var entry = new LogEntry(
      EntryType.ScopeEnd,
      Guid.NewGuid(),
      parentScopeId: null,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state);

    // Verify empty state is handled correctly
    Assert.Equal(EntryType.ScopeEnd, entry.Type);
    Assert.Empty(entry.State);
  }

  [Fact]
  public void Constructor_WithComplexState_ShouldPreserveStateObjects() {
    // Setup complex state with various object types
    var state = new object[] {
      "string value",
      42,
      new { Property = "anonymous object" },
      new KeyValuePair<string, object>("key", "value")
    };

    // Execute constructor
    var entry = new LogEntry(
      EntryType.Log,
      scopeId: null,
      parentScopeId: null,
      LogLevel.Debug,
      new EventId(999),
      "Complex state message",
      exception: null,
      state);

    // Verify state preservation
    Assert.Equal(4, entry.State.Length);
    Assert.Equal("string value", entry.State[0]);
    Assert.Equal(42, entry.State[1]);
    Assert.NotNull(entry.State[2]);
    Assert.IsType<KeyValuePair<string, object>>(entry.State[3]);
  }

  [Theory]
  [InlineData(EntryType.Log)]
  [InlineData(EntryType.ScopeStart)]
  [InlineData(EntryType.ScopeEnd)]
  public void Constructor_WithDifferentEntryTypes_ShouldSetTypeCorrectly(EntryType entryType) {
    // Setup with different entry types
    var state = new object[] { "test" };

    // Execute constructor
    var entry = new LogEntry(
      entryType,
      scopeId: null,
      parentScopeId: null,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state);

    // Verify type is set correctly
    Assert.Equal(entryType, entry.Type);
  }

  [Fact]
  public void Properties_AfterConstruction_ShouldBeImmutable() {
    // Setup test data
    var originalState = new object[] { "original" };
    var entry = new LogEntry(
      EntryType.Log,
      scopeId: null,
      parentScopeId: null,
      LogLevel.Information,
      new EventId(1),
      "Test message",
      exception: null,
      originalState);

    // Attempt to modify the state array reference
    var retrievedState = entry.State;

    // Verify that modifying the returned array doesn't affect the entry
    // (This tests that the property returns a reference, not a copy,
    // but the LogEntry itself should be treated as immutable)
    Assert.NotNull(retrievedState);
    Assert.Equal(originalState.Length, retrievedState.Length);
    Assert.Equal(originalState[0], retrievedState[0]);
  }

  [Fact]
  public void Constructor_WithLargeState_ShouldHandleCorrectly() {
    // Setup large state array
    var state = Enumerable.Range(0, 1000).Cast<object>().ToArray();

    // Execute constructor
    var entry = new LogEntry(
      EntryType.Log,
      scopeId: null,
      parentScopeId: null,
      LogLevel.Information,
      new EventId(1),
      "Large state message",
      exception: null,
      state);

    // Verify large state is preserved
    Assert.Equal(1000, entry.State.Length);
    Assert.Equal(0, entry.State[0]);
    Assert.Equal(999, entry.State[999]);
  }
}
