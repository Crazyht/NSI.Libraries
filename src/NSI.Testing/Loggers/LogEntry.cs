using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers;

/// <summary>
/// Immutable captured logging record (message or scope lifecycle marker) used in tests.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Represent either a structured log message or a scope start/end boundary.</description></item>
///   <item><description>Preserve correlation data (scope hierarchy via <see cref="ScopeId"/> /
///     <see cref="ParentScopeId"/>).</description></item>
///   <item><description>Retain structured state (<see cref="State"/>), exceptions and event metadata
///     for deterministic assertions.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description><see cref="Type"/> determines semantic interpretation of other nullable
///     members (e.g. <see cref="Level"/>, <see cref="EventId"/>, <see cref="Message"/> are null for
///     scope boundaries).</description></item>
///   <item><description>Do not mutate the array returned by <see cref="State"/>; treat as immutable
///     snapshot (logger infrastructure owns creation).</description></item>
///   <item><description>Prefer helper filters (e.g. extension methods) instead of manual predicates
///     in test code for readability.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Allocation cost is small and proportional to structured state size.</description></item>
///   <item><description>No further allocations after construction (pure data carrier).</description></item>
///   <item><description>Debugger display optimized for quick triage.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Fully immutable; safe for concurrent reads across parallel test runs.</para>
/// <para>Scope Semantics: A logical scope lifecycle is represented by a pair of entries with
/// <see cref="EntryType.ScopeStart"/> and <see cref="EntryType.ScopeEnd"/> sharing the same
/// <see cref="ScopeId"/>. Hierarchy is derived from <see cref="ParentScopeId"/>.</para>
/// </remarks>
/// <example>
/// <code>
/// // Assert a single error log with specific event id
/// var error = store.GetAll()
///   .Where(e => e.Type == EntryType.Log &amp;&amp; e.Level == LogLevel.Error)
///   .Single();
/// Assert.Equal(42, error.EventId?.Id);
/// Assert.Contains("FAILED", error.Message, StringComparison.Ordinal);
///
/// // Validate scope pairing
/// var starts = store.GetAll().Count(e => e.Type == EntryType.ScopeStart);
/// var ends = store.GetAll().Count(e => e.Type == EntryType.ScopeEnd);
/// Assert.Equal(starts, ends);
/// </code>
/// </example>
/// <seealso cref="EntryType"/>
/// <seealso cref="ILogEntryStore"/>
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public sealed class LogEntry {
  /// <summary>Entry classification (log message, scope start, scope end).</summary>
  public EntryType Type { get; }

  /// <summary>Current scope identifier or null when outside any scope.</summary>
  public Guid? ScopeId { get; }

  /// <summary>Parent scope identifier or null when root-level or a loose log.</summary>
  public Guid? ParentScopeId { get; }

  /// <summary>Log level for message entries; null for scope boundaries.</summary>
  public LogLevel? Level { get; }

  /// <summary>Event identifier for message entries; null for scope boundaries.</summary>
  public EventId? EventId { get; }

  /// <summary>Formatted message for log entries; null for scope boundaries.</summary>
  public string? Message { get; }

  /// <summary>Associated exception instance if present; otherwise null.</summary>
  public Exception? Exception { get; }

  /// <summary>Structured state (logging values or scope variables) – never null.</summary>
  [SuppressMessage(
    "Performance",
    "CA1819:Properties should not return arrays",
    Justification = "Array chosen to mirror ILogger pipeline shape & avoid per-access copying.")]
  public object[] State { get; }

  /// <summary>
  /// Creates a new immutable log entry (message or scope record).
  /// </summary>
  /// <param name="type">Classification of entry.</param>
  /// <param name="scopeId">Scope identifier (or null for non-scoped log).</param>
  /// <param name="parentScopeId">Parent scope identifier for nested scopes.</param>
  /// <param name="level">Log level (null for scope entries).</param>
  /// <param name="eventId">Event identifier (null for scope entries).</param>
  /// <param name="message">Formatted message (null for scope entries).</param>
  /// <param name="exception">Captured exception if any.</param>
  /// <param name="state">Structured state array (non-null).</param>
  /// <exception cref="ArgumentNullException">When <paramref name="state"/> is null.</exception>
  /// <remarks>
  /// <para>No normalization is performed; caller supplies data exactly as captured from the logger
  /// pipeline.</para>
  /// <para>For scope entries (<see cref="EntryType.ScopeStart"/> / <see cref="EntryType.ScopeEnd"/>)
  /// only <see cref="Type"/>, <see cref="ScopeId"/>, <see cref="ParentScopeId"/> and
  /// <see cref="State"/> should typically have values.</para>
  /// </remarks>
  [SuppressMessage(
    "Major Code Smell",
    "S107:Methods should not have too many parameters",
    Justification = "Comprehensive immutable value object initialization – all data required.")]
  public LogEntry(
    EntryType type,
    Guid? scopeId,
    Guid? parentScopeId,
    LogLevel? level,
    EventId? eventId,
    string? message,
    Exception? exception,
    object[] state) {
    ArgumentNullException.ThrowIfNull(state);

    Type = type;
    ScopeId = scopeId;
    ParentScopeId = parentScopeId;
    Level = level;
    EventId = eventId;
    Message = message;
    Exception = exception;
    State = state;
  }

  private string GetDebuggerDisplay() => Type switch {
    EntryType.Log => $"Log [{Level?.ToString() ?? "None"}] {Message ?? "<no message>"}" +
                     (Exception != null ? $" | Ex: {Exception.GetType().Name}" : string.Empty),
    EntryType.ScopeStart => $"ScopeStart [Id={ScopeId}]",
    EntryType.ScopeEnd => $"ScopeEnd [Id={ScopeId}]",
    _ => $"EntryType={Type}"
  };
}
