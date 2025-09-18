using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers;

/// <summary>
/// High-level domain extension helpers for querying <see cref="LogEntry"/> sequences in tests.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Provide expressive, composable filters over <see cref="IEnumerable{T}"/> of
///     <see cref="LogEntry"/> without hiding underlying LINQ semantics.</description></item>
///   <item><description>Encapsulate common log / scope analysis patterns (levels, messages, scopes,
///     exceptions, correlation).</description></item>
///   <item><description>Improve test readability by replacing verbose predicate logic with intent
///     revealing method calls.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>All methods return deferred (lazy) LINQ queries; materialize with
///     <c>.ToList()</c> / <c>.Single()</c> in assertions.</description></item>
///   <item><description>Filters targeting log entries automatically exclude scope boundaries.</description></item>
///   <item><description>Prefer chaining multiple small filters instead of writing complex
///     predicates inline.</description></item>
///   <item><description>Regular expression based filtering (<see cref="WithMessageMatch"/>) should be
///     used sparingly (higher cost than simple string operations).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>All filters are O(n) with minimal allocations (only iterator objects).</description></item>
///   <item><description>Regex filtering cost depends on pattern complexity; precompile heavy
///     patterns and reuse the <see cref="Regex"/> instance.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Extension methods themselves are stateless; thread-safety depends on the
/// underlying sequence enumeration (snapshot recommended via store <c>GetAll()</c>).</para>
/// </remarks>
/// <example>
/// <code>
/// // Basic error extraction
/// var errors = store.GetAll()
///   .LogsOnly()
///   .WithLogLevel(LogLevel.Error)
///   .ToList();
///
/// // Detect repeated identical messages (potential flooding)
/// var repeated = store.GetAll()
///   .LogsOnly()
///   .GroupBy(e => e.Message)
///   .Where(g => g.Count() > 10)
///   .Select(g => new { Message = g.Key, Count = g.Count() })
///   .ToList();
///
/// // Find all entries inside a scope identified by variable UserId=42
/// var userScopeIds = store.GetAll()
///   .ScopeStartsOnly()
///   .WithScopeContainingVar("UserId", 42)
///   .Select(s => s.ScopeId!.Value)
///   .ToList();
/// var userEntries = store.GetAll()
///   .Where(e => e.ScopeId.HasValue &amp;&amp; userScopeIds.Contains(e.ScopeId.Value))
///   .ToList();
/// </code>
/// </example>
/// <seealso cref="LogEntry"/>
/// <seealso cref="EntryType"/>
/// <seealso cref="ILogEntryStore"/>
public static class LogEntryStoreExtensions {
  /// <summary>Filters to log message entries only (excludes scope markers).</summary>
  /// <param name="entries">Source sequence (non-null).</param>
  /// <returns>Deferred sequence of log entries.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="entries"/> is null.</exception>
  public static IEnumerable<LogEntry> LogsOnly(this IEnumerable<LogEntry> entries) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.Log);
  }

  /// <summary>Filters to scope start entries.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <returns>Deferred sequence of scope start entries.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="entries"/> is null.</exception>
  public static IEnumerable<LogEntry> ScopeStartsOnly(this IEnumerable<LogEntry> entries) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.ScopeStart);
  }

  /// <summary>Filters to scope end entries.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <returns>Deferred sequence of scope end entries.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="entries"/> is null.</exception>
  public static IEnumerable<LogEntry> ScopeEndsOnly(this IEnumerable<LogEntry> entries) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.ScopeEnd);
  }

  /// <summary>Filters log entries to a specific <see cref="LogLevel"/>.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="level">Desired log level.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="entries"/> is null.</exception>
  public static IEnumerable<LogEntry> WithLogLevel(
    this IEnumerable<LogEntry> entries,
    LogLevel level) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.Log && e.Level == level);
  }

  /// <summary>Filters log entries whose message matches a regular expression.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="regex">Precompiled or ad-hoc regex (non-null).</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When args are null.</exception>
  /// <remarks>Use compiled regex for repeated heavy usage.</remarks>
  public static IEnumerable<LogEntry> WithMessageMatch(
    this IEnumerable<LogEntry> entries,
    Regex regex) {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentNullException.ThrowIfNull(regex);
    return entries.Where(e => e.Type == EntryType.Log &&
                              regex.IsMatch(e.Message ?? string.Empty));
  }

  /// <summary>Filters log entries containing the provided text.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="text">Substring to locate (non-null).</param>
  /// <param name="comparison">Comparison strategy (default OrdinalIgnoreCase).</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When args are null.</exception>
  public static IEnumerable<LogEntry> WithMessageContains(
    this IEnumerable<LogEntry> entries,
    string text,
    StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentNullException.ThrowIfNull(text);
    return entries.Where(e => e.Type == EntryType.Log &&
                              (e.Message?.Contains(text, comparison) ?? false));
  }

  /// <summary>Filters log entries whose message starts with the given prefix.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="text">Prefix text.</param>
  /// <param name="comparison">Comparison strategy.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When args are null.</exception>
  public static IEnumerable<LogEntry> WithMessageStartsWith(
    this IEnumerable<LogEntry> entries,
    string text,
    StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentNullException.ThrowIfNull(text);
    return entries.Where(e => e.Type == EntryType.Log &&
                              (e.Message?.StartsWith(text, comparison) ?? false));
  }

  /// <summary>Filters log entries whose message ends with the given suffix.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="text">Suffix text.</param>
  /// <param name="comparison">Comparison strategy.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When args are null.</exception>
  public static IEnumerable<LogEntry> WithMessageEndsWith(
    this IEnumerable<LogEntry> entries,
    string text,
    StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentNullException.ThrowIfNull(text);
    return entries.Where(e => e.Type == EntryType.Log &&
                              (e.Message?.EndsWith(text, comparison) ?? false));
  }

  /// <summary>Filters scope start entries containing an exact key/value variable.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="key">Variable key (non-empty).</param>
  /// <param name="value">Expected value (nullable allowed).</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  /// <exception cref="ArgumentException">When <paramref name="key"/> empty.</exception>
  public static IEnumerable<LogEntry> WithScopeContainingVar(
    this IEnumerable<LogEntry> entries,
    string key,
    object? value) {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentException.ThrowIfNullOrEmpty(key);
    return entries.Where(e => e.Type == EntryType.ScopeStart &&
                              e.State.OfType<KeyValuePair<string, object>>()
                                .Any(kv => kv.Key == key &&
                                         ((kv.Value?.Equals(value)) ?? (value == null))));
  }

  /// <summary>Filters scope start entries that contain a variable key (any value).</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="key">Variable key.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  /// <exception cref="ArgumentException">When <paramref name="key"/> empty.</exception>
  public static IEnumerable<LogEntry> WithScopeContainingKey(
    this IEnumerable<LogEntry> entries,
    string key) {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentException.ThrowIfNullOrEmpty(key);
    return entries.Where(e => e.Type == EntryType.ScopeStart &&
                              e.State.OfType<KeyValuePair<string, object>>()
                                .Any(kv => kv.Key == key));
  }

  /// <summary>Filters log entries by exact <see cref="EventId"/>.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="eventId">Event identifier struct.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithEventId(
    this IEnumerable<LogEntry> entries,
    EventId eventId) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.Log && e.EventId == eventId);
  }

  /// <summary>Filters log entries by <see cref="EventId.Id"/> value.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="eventId">Event id integer value.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithEventId(
    this IEnumerable<LogEntry> entries,
    int eventId) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.Log && e.EventId?.Id == eventId);
  }

  /// <summary>Filters log entries that include any exception.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithException(
    this IEnumerable<LogEntry> entries) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.Log && e.Exception != null);
  }

  /// <summary>Filters log entries whose exception is (or derives from) <typeparamref name="TException"/>.</summary>
  /// <typeparam name="TException">Exception type filter.</typeparam>
  /// <param name="entries">Source sequence.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithException<TException>(
    this IEnumerable<LogEntry> entries)
    where TException : Exception {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.Type == EntryType.Log && e.Exception is TException);
  }

  /// <summary>Filters entries that occurred within a specific scope id.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="scopeId">Scope identifier.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithinScope(
    this IEnumerable<LogEntry> entries,
    Guid scopeId) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.ScopeId == scopeId);
  }

  /// <summary>Filters entries that are direct children of a parent scope id.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <param name="parentScopeId">Parent scope identifier.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithParentScope(
    this IEnumerable<LogEntry> entries,
    Guid parentScopeId) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.ParentScopeId == parentScopeId);
  }

  /// <summary>Filters entries that are not associated with any scope.</summary>
  /// <param name="entries">Source sequence.</param>
  /// <returns>Deferred filtered sequence.</returns>
  /// <exception cref="ArgumentNullException">When entries is null.</exception>
  public static IEnumerable<LogEntry> WithoutScope(
    this IEnumerable<LogEntry> entries) {
    ArgumentNullException.ThrowIfNull(entries);
    return entries.Where(e => e.ScopeId == null);
  }
}
