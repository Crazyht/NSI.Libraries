using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers {

  /// <summary>
  /// Represents a single logging entry or scope operation.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class encapsulates all information about a logging operation,
  /// including standard log messages and scope management operations.
  /// It serves as the fundamental data structure for capturing and
  /// analyzing logging behavior in tests.
  /// </para>
  /// <para>
  /// For scope entries, the <see cref="Level"/> and <see cref="EventId"/> 
  /// properties will be null, while scope-specific information is stored 
  /// in the <see cref="State"/> property.
  /// </para>
  /// <para>
  /// The class maintains immutability after construction to ensure
  /// thread-safe access and prevent accidental modifications during
  /// test execution.
  /// </para>
  /// </remarks>
  [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
  public sealed class LogEntry {
    /// <summary>
    /// Gets the type of this log entry.
    /// </summary>
    /// <value>
    /// An <see cref="EntryType"/> value indicating whether this is a log message,
    /// scope start, or scope end entry.
    /// </value>
    public EntryType Type { get; }

    /// <summary>
    /// Gets the unique identifier of the scope this entry belongs to, 
    /// or <see langword="null"/> if outside any scope.
    /// </summary>
    /// <value>
    /// A <see cref="Guid"/> representing the scope identifier, or 
    /// <see langword="null"/> if this entry is not associated with any scope.
    /// </value>
    public Guid? ScopeId { get; }

    /// <summary>
    /// Gets the unique identifier of the parent scope, 
    /// or <see langword="null"/> if this is a root scope or log entry.
    /// </summary>
    /// <value>
    /// A <see cref="Guid"/> representing the parent scope identifier, or 
    /// <see langword="null"/> if this is a root-level entry.
    /// </value>
    public Guid? ParentScopeId { get; }

    /// <summary>
    /// Gets the log level for log entries, 
    /// or <see langword="null"/> for scope entries.
    /// </summary>
    /// <value>
    /// A <see cref="LogLevel"/> value for log entries, or 
    /// <see langword="null"/> for scope start and end entries.
    /// </value>
    public LogLevel? Level { get; }

    /// <summary>
    /// Gets the event identifier for log entries, 
    /// or <see langword="null"/> for scope entries.
    /// </summary>
    /// <value>
    /// An <see cref="EventId"/> value for log entries, or 
    /// <see langword="null"/> for scope start and end entries.
    /// </value>
    public EventId? EventId { get; }

    /// <summary>
    /// Gets the formatted log message for log entries, 
    /// or <see langword="null"/> for scope entries.
    /// </summary>
    /// <value>
    /// A string containing the formatted log message for log entries, or 
    /// <see langword="null"/> for scope start and end entries.
    /// </value>
    public string? Message { get; }

    /// <summary>
    /// Gets the exception associated with this log entry, 
    /// or <see langword="null"/> if no exception was logged.
    /// </summary>
    /// <value>
    /// The <see cref="Exception"/> instance associated with this log entry, or 
    /// <see langword="null"/> if no exception information is available.
    /// </value>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the state object array containing either the TState 
    /// for log entries or scope variables for scope entries.
    /// </summary>
    /// <value>
    /// An array of objects representing the structured logging state for log entries
    /// or the scope variables for scope entries. This array is never null.
    /// </value>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "By design with Logging.")]
    public object[] State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class.
    /// </summary>
    /// <param name="type">The type of log entry.</param>
    /// <param name="scopeId">The scope identifier, or <see langword="null"/> if outside any scope.</param>
    /// <param name="parentScopeId">The parent scope identifier, or <see langword="null"/> if root level.</param>
    /// <param name="level">The log level for log entries, or <see langword="null"/> for scope entries.</param>
    /// <param name="eventId">The event identifier for log entries, or <see langword="null"/> for scope entries.</param>
    /// <param name="message">The formatted message for log entries, or <see langword="null"/> for scope entries.</param>
    /// <param name="exception">The associated exception, or <see langword="null"/> if none.</param>
    /// <param name="state">The state object array containing log or scope data.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor validates that the state parameter is not null, as it is
    /// essential for both log entries (containing structured logging data) and
    /// scope entries (containing scope variables).
    /// </para>
    /// <para>
    /// All other parameters are optional and their values depend on the entry type.
    /// For log entries, level and eventId should typically be provided, while for
    /// scope entries, these values should be null.
    /// </para>
    /// </remarks>
    [SuppressMessage(
      "Major Code Smell",
      "S107:Methods should not have too many parameters",
      Justification = "All properties are readonly, so we need to pass everything in constructor.")]
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

    private string GetDebuggerDisplay() {
      if (Type == EntryType.Log) {
        var level = Level?.ToString() ?? "None";
        var msg = Message ?? "<no message>";
        var ex = Exception != null ? $" | Ex: {Exception.GetType().Name}" : "";
        return $"Log [{level}] {msg}{ex}";
      }
      if (Type == EntryType.ScopeStart) {
        return $"ScopeStart [Id={ScopeId}]";
      }
      if (Type == EntryType.ScopeEnd) {
        return $"ScopeEnd [Id={ScopeId}]";
      }
      return $"EntryType={Type}";
    }
  }

}
