using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers {

  /// <summary>
  /// Provides specialized extension methods for querying collections of log entries using LINQ.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class extends <see cref="IEnumerable{LogEntry}"/> with domain-specific
  /// methods that make log analysis more intuitive and readable. These methods
  /// are designed to work seamlessly with standard LINQ operations.
  /// </para>
  /// <para>
  /// The approach leverages the full power of LINQ while providing convenience methods
  /// for common logging analysis patterns. This combines the flexibility of LINQ
  /// expressions with the readability of domain-specific filters.
  /// </para>
  /// <para>
  /// Example usage patterns:
  /// <code>
  /// // Standard LINQ with domain extensions
  /// var errors = store.GetAll()
  ///   .WithLogLevel(LogLevel.Error)
  ///   .WithMessageContains("database")
  ///   .ToList();
  /// 
  /// // Complex LINQ queries
  /// var recentErrors = store.GetAll()
  ///   .Where(e => e.Type == EntryType.Log)
  ///   .WithLogLevel(LogLevel.Error)
  ///   .GroupBy(e => e.Message)
  ///   .Where(g => g.Count() > 5)
  ///   .Select(g => g.Key);
  /// 
  /// // Scope analysis with LINQ
  /// var scopeHierarchy = store.GetAll()
  ///   .ScopeStartsOnly()
  ///   .WithScopeContainingVar("UserId", 42)
  ///   .SelectMany(scope => store.GetAll().WithinScope(scope.ScopeId.Value))
  ///   .ToList();
  /// </code>
  /// </para>
  /// <para>
  /// The extensions are organized into several categories:
  /// <list type="bullet">
  ///   <item><description>Type filters - Filter by entry type (log, scope start, scope end)</description></item>
  ///   <item><description>Level filters - Filter log entries by log level</description></item>
  ///   <item><description>Message filters - Filter by message content using various strategies</description></item>
  ///   <item><description>Scope filters - Filter scope entries by variables and properties</description></item>
  ///   <item><description>Context filters - Filter by scope associations and hierarchy</description></item>
  ///   <item><description>Exception filters - Filter by exception presence and type</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public static class LogEntryEnumerableExtensions {
    /// <summary>
    /// Filters the sequence to log entries only, excluding scope operations.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <returns>A filtered sequence containing only log entries.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters out scope start and end entries, focusing only on
    /// actual log messages. This is useful when analyzing application logging
    /// behavior without scope management overhead.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> LogsOnly(this IEnumerable<LogEntry> entries) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.Log);
    }

    /// <summary>
    /// Filters the sequence to scope start entries only.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <returns>A filtered sequence containing only scope start entries.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters to scope start entries, which mark the beginning of
    /// logical operation scopes and contain scope variables.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> ScopeStartsOnly(this IEnumerable<LogEntry> entries) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.ScopeStart);
    }

    /// <summary>
    /// Filters the sequence to scope end entries only.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <returns>A filtered sequence containing only scope end entries.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters to scope end entries, which mark the completion of
    /// logical operation scopes.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> ScopeEndsOnly(this IEnumerable<LogEntry> entries) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.ScopeEnd);
    }

    /// <summary>
    /// Filters the sequence to entries with the specified log level.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="level">The log level to filter by.</param>
    /// <returns>A filtered sequence containing only entries with the specified log level.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters to log entries (not scope entries) that have the specified
    /// log level. It automatically excludes scope entries which don't have log levels.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithLogLevel(this IEnumerable<LogEntry> entries, LogLevel level) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.Log && e.Level == level);
    }

    /// <summary>
    /// Filters the sequence to entries whose message matches the specified regular expression.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="regex">The regular expression to match against log messages.</param>
    /// <returns>A filtered sequence containing only entries with matching messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> or <paramref name="regex"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method provides powerful pattern matching capabilities for log messages.
    /// It only applies to log entries (not scope entries) and uses the full power
    /// of .NET regular expressions.
    /// </para>
    /// <para>
    /// Example patterns:
    /// <code>
    /// // Match entries with timestamps
    /// entries.WithMessageMatch(new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"))
    /// 
    /// // Match entries with specific error codes
    /// entries.WithMessageMatch(new Regex(@"Error code: \d{4}"))
    /// </code>
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithMessageMatch(this IEnumerable<LogEntry> entries, Regex regex) {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(regex);
      return entries.Where(e => e.Type == EntryType.Log &&
                               regex.IsMatch(e.Message ?? string.Empty));
    }

    /// <summary>
    /// Filters the sequence to entries whose message contains the specified text.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="text">The text to search for in log messages.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>A filtered sequence containing only entries with messages containing the text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> or <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method provides simple text searching within log messages. It only
    /// applies to log entries (not scope entries) and supports various string
    /// comparison options including case-sensitive and case-insensitive matching.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithMessageContains(this IEnumerable<LogEntry> entries,
      string text, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(text);
      return entries.Where(e => e.Type == EntryType.Log &&
                               (e.Message?.Contains(text, comparison) ?? false));
    }

    /// <summary>
    /// Filters the sequence to entries whose message starts with the specified text.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="text">The text that messages should start with.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>A filtered sequence containing only entries with messages starting with the text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> or <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters log messages based on their prefix. It's useful for
    /// finding messages from specific components or operations that use consistent
    /// message formatting.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithMessageStartsWith(this IEnumerable<LogEntry> entries,
      string text, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(text);
      return entries.Where(e => e.Type == EntryType.Log &&
                               (e.Message?.StartsWith(text, comparison) ?? false));
    }

    /// <summary>
    /// Filters the sequence to entries whose message ends with the specified text.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="text">The text that messages should end with.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>A filtered sequence containing only entries with messages ending with the text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> or <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters log messages based on their suffix. It's useful for
    /// finding completion messages or messages with specific status indicators.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithMessageEndsWith(this IEnumerable<LogEntry> entries,
      string text, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(text);
      return entries.Where(e => e.Type == EntryType.Log &&
                               (e.Message?.EndsWith(text, comparison) ?? false));
    }

    /// <summary>
    /// Filters the sequence to scope entries containing the specified variable.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="key">The variable key to search for.</param>
    /// <param name="value">The variable value to match.</param>
    /// <returns>A filtered sequence containing only scope entries with the specified variable.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> or <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters scope start entries to find those containing a specific
    /// variable with the exact key and value. It's useful for finding scopes created
    /// with specific context information.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithScopeContainingVar(this IEnumerable<LogEntry> entries,
      string key, object? value) {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentException.ThrowIfNullOrEmpty(key);

      return entries.Where(e => e.Type == EntryType.ScopeStart &&
                               e.State.OfType<KeyValuePair<string, object>>()
                                 .Any(kv => kv.Key == key &&
                                           (kv.Value?.Equals(value) ?? value == null)));
    }

    /// <summary>
    /// Filters the sequence to scope entries containing a variable with the specified key.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="key">The variable key to search for.</param>
    /// <returns>A filtered sequence containing only scope entries with the specified key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> or <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters scope start entries to find those containing a variable
    /// with the specified key, regardless of the value.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithScopeContainingKey(this IEnumerable<LogEntry> entries,
      string key) {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentException.ThrowIfNullOrEmpty(key);

      return entries.Where(e => e.Type == EntryType.ScopeStart &&
                               e.State.OfType<KeyValuePair<string, object>>()
                                 .Any(kv => kv.Key == key));
    }

    /// <summary>
    /// Filters the sequence to entries with the specified event ID.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="eventId">The event ID to filter by.</param>
    /// <returns>A filtered sequence containing only entries with the specified event ID.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters log entries by their event ID. Event IDs are commonly
    /// used in structured logging to categorize different types of events.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithEventId(this IEnumerable<LogEntry> entries, EventId eventId) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.Log && e.EventId == eventId);
    }

    /// <summary>
    /// Filters the sequence to entries with the specified event ID value.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="eventId">The event ID value to filter by.</param>
    /// <returns>A filtered sequence containing only entries with the specified event ID value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method provides a convenient overload for filtering by event ID when
    /// you only have the integer value rather than an EventId struct.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithEventId(this IEnumerable<LogEntry> entries, int eventId) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.Log && e.EventId?.Id == eventId);
    }

    /// <summary>
    /// Filters the sequence to entries associated with an exception.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <returns>A filtered sequence containing only entries with associated exceptions.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters to log entries that have an associated exception.
    /// It's useful for finding error conditions or exceptional circumstances.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithException(this IEnumerable<LogEntry> entries) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.Log && e.Exception != null);
    }

    /// <summary>
    /// Filters the sequence to entries associated with an exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">The type of exception to filter by.</typeparam>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <returns>A filtered sequence containing only entries with exceptions of the specified type.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters to log entries that have an associated exception of
    /// the specified type. It uses type checking to match both exact types and
    /// derived types.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithException<TException>(this IEnumerable<LogEntry> entries)
      where TException : Exception {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.Type == EntryType.Log && e.Exception is TException);
    }

    /// <summary>
    /// Filters the sequence to entries within the specified scope.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="scopeId">The scope ID to filter by.</param>
    /// <returns>A filtered sequence containing only entries within the specified scope.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters entries to those that occurred within the specified scope.
    /// It matches entries where the ScopeId property equals the provided scope ID.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithinScope(this IEnumerable<LogEntry> entries, Guid scopeId) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.ScopeId == scopeId);
    }

    /// <summary>
    /// Filters the sequence to entries that are direct children of the specified parent scope.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <param name="parentScopeId">The parent scope ID to filter by.</param>
    /// <returns>A filtered sequence containing only entries with the specified parent scope.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters entries to those that are direct children of the specified
    /// parent scope. It's useful for analyzing scope hierarchy.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithParentScope(this IEnumerable<LogEntry> entries, Guid parentScopeId) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.ParentScopeId == parentScopeId);
    }

    /// <summary>
    /// Filters the sequence to entries that occurred outside of any scope.
    /// </summary>
    /// <param name="entries">The sequence of log entries to filter.</param>
    /// <returns>A filtered sequence containing only entries outside of any scope.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters to entries that were logged outside of any scope context.
    /// These are typically root-level log messages.
    /// </para>
    /// </remarks>
    public static IEnumerable<LogEntry> WithoutScope(this IEnumerable<LogEntry> entries) {
      ArgumentNullException.ThrowIfNull(entries);
      return entries.Where(e => e.ScopeId == null);
    }
  }

}
