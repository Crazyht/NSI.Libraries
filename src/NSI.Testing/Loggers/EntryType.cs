namespace NSI.Testing.Loggers;

/// <summary>
/// Classifies persisted log store entries (log messages and scope lifecycle markers).
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Differentiate standard log messages from scope boundaries.</description></item>
///   <item><description>Enable targeted filtering / queries in test assertions and benchmarks.</description></item>
///   <item><description>Preserve scope hierarchy by pairing <see cref="ScopeStart"/> and
///     <see cref="ScopeEnd"/> entries.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Always treat <see cref="ScopeStart"/> / <see cref="ScopeEnd"/> as structural, not
///     semantic log content.</description></item>
///   <item><description>Use extension helpers (e.g. <c>LogsOnly()</c>, <c>ScopeStartsOnly()</c>) instead of
///     manual filtering for readability.</description></item>
///   <item><description>Assert scope pairing in integration tests when validating contextual logging.</description></item>
/// </list>
/// </para>
/// <para>Performance: Enum comparisons are constant time; negligible overhead in filtering
/// pipelines relative to message parsing or regex operations.</para>
/// <para>Thread-safety: Enumeration is immutable; safe for concurrent access.</para>
/// </remarks>
/// <example>
/// <code>
/// // Filtering only log message entries in a test
/// var logEntries = store.GetAll().Where(e => e.Type == EntryType.Log).ToList();
///
/// // Counting active scopes (starts minus ends)
/// var starts = store.GetAll().Count(e => e.Type == EntryType.ScopeStart);
/// var ends = store.GetAll().Count(e => e.Type == EntryType.ScopeEnd);
/// var active = starts - ends;
/// </code>
/// </example>
public enum EntryType {
  /// <summary>Represents a standard log message.</summary>
  /// <remarks>
  /// <para>Contains user/application logging data: level, template, structured state, optional
  /// event id and exception information.</para>
  /// </remarks>
  Log,

  /// <summary>Represents the beginning of a logging scope.</summary>
  /// <remarks>
  /// <para>Captures scope variables and establishes hierarchical parent linkage. Produced when a
  /// scope is entered (e.g. <c>logger.BeginScope(...)</c>).</para>
  /// </remarks>
  ScopeStart,

  /// <summary>Represents the end of a logging scope.</summary>
  /// <remarks>
  /// <para>Emitted when a scope's <c>IDisposable</c> is disposed, closing the structural region
  /// started by its corresponding <see cref="ScopeStart"/> entry.</para>
  /// </remarks>
  ScopeEnd
}
