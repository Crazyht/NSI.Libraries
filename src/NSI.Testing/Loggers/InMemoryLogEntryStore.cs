namespace NSI.Testing.Loggers;

/// <summary>
/// In-memory, thread-safe implementation of <see cref="ILogEntryStore"/> for test logging capture.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Capture log and scope entries preserving strict insertion order.</description></item>
///   <item><description>Provide snapshot style retrieval for deterministic assertions.</description></item>
///   <item><description>Offer fast reset between test cases via <see cref="Clear"/>.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Call <see cref="Clear"/> in test fixture teardown to avoid cross‑test bleed.</description></item>
///   <item><description>Prefer querying helpers / extension methods over ad-hoc filtering for readability.</description></item>
///   <item><description>Use in benchmarks to evaluate logging allocation patterns without external sinks.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description><see cref="Add(LogEntry)"/> amortized O(1).</description></item>
///   <item><description><see cref="GetAll"/> O(n) copying to a new list snapshot.</description></item>
///   <item><description>Lock contention minimal (short critical sections).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: All public members are safe for concurrent invocation; internal state guarded by
/// a private lock instance (not externally observable).</para>
/// <para>Memory: Linear growth with entry count; snapshots allocate per call. Clear periodically for
/// long-running suites.</para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryLogEntryStore();
/// var logger = new MockLogger&lt;Sample&gt;(store);
/// logger.LogInformation("Processing {Id}", 42);
/// var entries = store.GetAll();
/// Assert.Single(entries);
/// Assert.Equal(EntryType.Log, entries[0].Type);
/// store.Clear();
/// Assert.Empty(store.GetAll());
/// </code>
/// </example>
/// <seealso cref="ILogEntryStore"/>
/// <seealso cref="LogEntry"/>
/// <seealso cref="EntryType"/>
public sealed class InMemoryLogEntryStore: ILogEntryStore {
  private readonly List<LogEntry> _Entries = [];
  private readonly Lock _Lock = new();

  /// <summary>Adds a log or scope entry preserving chronological sequence.</summary>
  /// <param name="entry">Non-null fully populated <see cref="LogEntry"/>.</param>
  /// <exception cref="ArgumentNullException">When <paramref name="entry"/> is null.</exception>
  /// <remarks>Lock held only for list append (minimal contention window).</remarks>
  public void Add(LogEntry entry) {
    ArgumentNullException.ThrowIfNull(entry);
    lock (_Lock) {
      _Entries.Add(entry);
    }
  }

  /// <summary>Returns a point-in-time snapshot of all stored entries.</summary>
  /// <returns>Immutable (caller-safe) list ordered by insertion; never null.</returns>
  /// <remarks>Subsequent calls after additional <see cref="Add(LogEntry)"/> operations yield
  /// independent snapshots (no shared mutable state).</remarks>
  public IReadOnlyList<LogEntry> GetAll() {
    lock (_Lock) {
      return [.. _Entries];
    }
  }

  /// <summary>Clears all entries atomically.</summary>
  /// <remarks>After completion, <see cref="GetAll"/> returns an empty collection until further
  /// <see cref="Add(LogEntry)"/> calls occur.</remarks>
  public void Clear() {
    lock (_Lock) {
      _Entries.Clear();
    }
  }
}
