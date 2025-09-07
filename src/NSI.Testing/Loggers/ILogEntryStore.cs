
namespace NSI.Testing.Loggers;

/// <summary>
/// Provides methods for storing and retrieving log entries.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be thread-safe to support parallel test execution.
/// The store maintains the chronological order of log entries and scope operations,
/// ensuring that the sequence of logging events can be accurately reproduced
/// and analyzed during test verification.
/// </para>
/// <para>
/// This interface serves as the foundation for centralizing log capture across
/// multiple logger instances, enabling comprehensive testing scenarios where
/// multiple components may be logging simultaneously.
/// </para>
/// <para>
/// Key design principles:
/// <list type="bullet">
///   <item><description>Thread-safe operations for concurrent access</description></item>
///   <item><description>Chronological ordering preservation</description></item>
///   <item><description>Efficient retrieval for test assertions</description></item>
///   <item><description>Memory management for long-running tests</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ILogEntryStore {
  /// <summary>
  /// Adds a log entry to the store.
  /// </summary>
  /// <param name="entry">The log entry to add.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="entry"/> is <see langword="null"/>.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method must be thread-safe and should preserve the chronological
  /// order of entries as they are added. The implementation should ensure
  /// that concurrent calls to this method do not result in lost entries
  /// or corrupted state.
  /// </para>
  /// <para>
  /// Entries are typically added by <see cref="MockLogger{T}"/> instances
  /// during test execution, capturing both regular log messages and scope
  /// lifecycle events.
  /// </para>
  /// </remarks>
  public void Add(LogEntry entry);

  /// <summary>
  /// Gets all log entries in chronological order.
  /// </summary>
  /// <returns>
  /// A read-only list of all stored log entries, ordered by the time they were added.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method should return a snapshot of all entries at the time of the call,
  /// ensuring that the returned collection remains stable even if new entries
  /// are added after the method returns.
  /// </para>
  /// <para>
  /// The returned collection should be read-only to prevent accidental
  /// modifications that could affect test reliability.
  /// </para>
  /// </remarks>
  public IReadOnlyList<LogEntry> GetAll();

  /// <summary>
  /// Removes all log entries from the store.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method is typically called between test methods or test classes
  /// to ensure a clean state for each test execution. It should be thread-safe
  /// and ensure that concurrent operations are handled gracefully.
  /// </para>
  /// <para>
  /// After calling this method, <see cref="GetAll"/> should return an empty
  /// collection until new entries are added.
  /// </para>
  /// </remarks>
  public void Clear();
}
