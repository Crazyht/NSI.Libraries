using Microsoft.Extensions.Logging;
using NSI.Testing.Loggers;

namespace NSI.Testing.Tests.TestUtilities;
/// <summary>
/// Factory class for creating test LogEntry instances with predefined configurations.
/// </summary>
/// <remarks>
/// <para>
/// This factory simplifies test data creation and ensures consistent test scenarios
/// across different test classes. It provides methods for creating common log entry
/// patterns used in testing.
/// </para>
/// </remarks>
internal static class LogEntryFactory {

  /// <summary>
  /// Creates a standard log entry with the specified parameters.
  /// </summary>
  /// <param name="level">The log level for the entry.</param>
  /// <param name="message">The log message text.</param>
  /// <param name="scopeId">Optional scope identifier.</param>
  /// <param name="parentScopeId">Optional parent scope identifier.</param>
  /// <param name="eventId">Optional event identifier.</param>
  /// <param name="exception">Optional exception to associate.</param>
  /// <returns>A configured log entry for testing.</returns>
  public static LogEntry CreateLogEntry(
    LogLevel level = LogLevel.Information,
    string message = "Test message",
    Guid? scopeId = null,
    Guid? parentScopeId = null,
    EventId? eventId = null,
    Exception? exception = null) =>
    new(
      EntryType.Log,
      scopeId,
      parentScopeId,
      level,
      eventId ?? new EventId(1, "TestEvent"),
      message,
      exception,
      [message]);

  /// <summary>
  /// Creates a scope start entry with the specified variables.
  /// </summary>
  /// <param name="scopeId">The unique scope identifier.</param>
  /// <param name="parentScopeId">Optional parent scope identifier.</param>
  /// <param name="variables">Dictionary of scope variables.</param>
  /// <returns>A scope start entry for testing.</returns>
  public static LogEntry CreateScopeStart(
    Guid? scopeId = null,
    Guid? parentScopeId = null,
    Dictionary<string, object>? variables = null) {

    var actualScopeId = scopeId ?? Guid.NewGuid();
    var scopeVariables = variables ?? new Dictionary<string, object> {
      { "TestKey", "TestValue" }
    };
    var state = scopeVariables
      .Select(kv => (object)new KeyValuePair<string, object>(kv.Key, kv.Value))
      .ToArray();

    return new LogEntry(
      EntryType.ScopeStart,
      actualScopeId,
      parentScopeId,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state);
  }

  /// <summary>
  /// Creates a scope end entry with the specified scope identifier.
  /// </summary>
  /// <param name="scopeId">The scope identifier that is ending.</param>
  /// <returns>A scope end entry for testing.</returns>
  public static LogEntry CreateScopeEnd(Guid scopeId) =>
    new(
      EntryType.ScopeEnd,
      scopeId,
      parentScopeId: null,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      []);

  /// <summary>
  /// Creates a complete scope lifecycle with start, logs, and end entries.
  /// </summary>
  /// <param name="scopeId">The scope identifier to use.</param>
  /// <param name="parentScopeId">Optional parent scope identifier.</param>
  /// <param name="variables">Dictionary of scope variables.</param>
  /// <param name="logCount">Number of log entries to create within the scope.</param>
  /// <returns>Collection of log entries representing a complete scope lifecycle.</returns>
  public static IEnumerable<LogEntry> CreateScopeLifecycle(
    Guid? scopeId = null,
    Guid? parentScopeId = null,
    Dictionary<string, object>? variables = null,
    int logCount = 2) {

    var actualScopeId = scopeId ?? Guid.NewGuid();
    var entries = new List<LogEntry> {
      // Add scope start
      CreateScopeStart(actualScopeId, parentScopeId, variables)
    };

    // Add log entries within the scope
    for (var i = 0; i < logCount; i++) {
      entries.Add(CreateLogEntry(
        level: LogLevel.Information,
        message: $"Log message {i + 1}",
        scopeId: actualScopeId,
        parentScopeId: parentScopeId,
        eventId: new EventId(i + 100, $"Event{i + 1}")));
    }

    // Add scope end
    entries.Add(CreateScopeEnd(actualScopeId));

    return entries;
  }

  /// <summary>
  /// Creates entries for nested scope hierarchy testing.
  /// </summary>
  /// <param name="depth">The depth of nested scopes to create.</param>
  /// <returns>Collection of log entries representing nested scopes.</returns>
  public static IEnumerable<LogEntry> CreateNestedScopes(int depth = 3) {
    var entries = new List<LogEntry>();
    var scopeIds = new List<Guid>();

    // Create nested scope starts
    for (var i = 0; i < depth; i++) {
      var scopeId = Guid.NewGuid();
      var parentId = i > 0 ? scopeIds[i - 1] : (Guid?)null;

      scopeIds.Add(scopeId);
      entries.Add(CreateScopeStart(
        scopeId,
        parentId,
        new Dictionary<string, object> { { $"Level{i}", $"Value{i}" } }));

      // Add a log in each scope
      entries.Add(CreateLogEntry(
        message: $"Message at level {i}",
        scopeId: scopeId,
        parentScopeId: parentId));
    }

    // Create scope ends (in reverse order)
    for (var i = depth - 1; i >= 0; i--) {
      entries.Add(CreateScopeEnd(scopeIds[i]));
    }

    return entries;
  }

  /// <summary>
  /// Creates a collection of log entries with various log levels for testing.
  /// </summary>
  /// <param name="includeException">Whether to include entries with exceptions.</param>
  /// <returns>Collection of log entries with different log levels.</returns>
  public static IEnumerable<LogEntry> CreateVariousLogLevels(bool includeException = true) {
    var entries = new List<LogEntry> {
      CreateLogEntry(LogLevel.Trace, "Trace message"),
      CreateLogEntry(LogLevel.Debug, "Debug message"),
      CreateLogEntry(LogLevel.Information, "Information message"),
      CreateLogEntry(LogLevel.Warning, "Warning message"),
      CreateLogEntry(LogLevel.Error, "Error message"),
      CreateLogEntry(LogLevel.Critical, "Critical message")
    };

    if (includeException) {
      entries.Add(CreateLogEntry(
        LogLevel.Error,
        "Error with exception",
        exception: new InvalidOperationException("Test exception")));
    }

    return entries;
  }
}
