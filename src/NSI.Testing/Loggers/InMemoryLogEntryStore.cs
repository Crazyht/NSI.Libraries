

namespace NSI.Testing.Loggers;

/// <summary>
/// Provides an in-memory implementation of <see cref="ILogEntryStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is thread-safe and maintains entries in the order they were added.
/// It uses internal locking mechanisms to ensure safe concurrent access from multiple
/// threads, making it suitable for use in parallel test execution scenarios.
/// </para>
/// <para>
/// The store uses a simple <see cref="List{T}"/> internally for efficient sequential
/// access patterns typical in test scenarios. Memory usage grows linearly with the
/// number of log entries, so it's recommended to call <see cref="Clear"/> between
/// test methods in long-running test suites.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item><description>Thread-safe operations using lock-based synchronization</description></item>
///   <item><description>Preserves chronological order of log entries</description></item>
///   <item><description>Efficient for typical test scenarios (hundreds to thousands of entries)</description></item>
///   <item><description>Snapshot-based retrieval prevents collection modification issues</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class InMemoryLogEntryStore: ILogEntryStore {
  private readonly List<LogEntry> _Entries = [];
  private readonly Lock _Lock = new();

  /// <summary>
  /// Adds a log entry to the store in a thread-safe manner.
  /// </summary>
  /// <param name="entry">The log entry to add.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="entry"/> is <see langword="null"/>.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method uses internal locking to ensure thread-safe access to the
  /// underlying collection. The entry is added to the end of the internal
  /// list, preserving chronological order.
  /// </para>
  /// <para>
  /// The operation has O(1) amortized time complexity for typical usage
  /// patterns where the internal list doesn't need to resize frequently.
  /// </para>
  /// </remarks>
  public void Add(LogEntry entry) {
    ArgumentNullException.ThrowIfNull(entry);

    lock (_Lock) {
      _Entries.Add(entry);
    }
  }

  /// <summary>
  /// Gets a snapshot of all log entries in chronological order.
  /// </summary>
  /// <returns>
  /// A read-only list containing copies of all stored log entries.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method creates a snapshot of the current entries at the time of the call.
  /// The returned collection is independent of the internal storage, so subsequent
  /// additions to the store will not affect the returned collection.
  /// </para>
  /// <para>
  /// The operation has O(n) time complexity where n is the number of stored entries,
  /// as it creates a new list containing all current entries.
  /// </para>
  /// <para>
  /// Thread safety is ensured through internal locking, but the returned collection
  /// itself is not synchronized as it represents a point-in-time snapshot.
  /// </para>
  /// </remarks>
  public IReadOnlyList<LogEntry> GetAll() {
    lock (_Lock) {
      return [.. _Entries];
    }
  }

  /// <summary>
  /// Removes all log entries from the store.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method removes all entries from the internal storage in a thread-safe manner.
  /// After this method completes, <see cref="GetAll"/> will return an empty collection
  /// until new entries are added.
  /// </para>
  /// <para>
  /// The operation is atomic - either all entries are cleared or none are, ensuring
  /// consistent state even under concurrent access scenarios.
  /// </para>
  /// <para>
  /// It's recommended to call this method between test methods to ensure each test
  /// starts with a clean log state.
  /// </para>
  /// </remarks>
  public void Clear() {
    lock (_Lock) {
      _Entries.Clear();
    }
  }
}
