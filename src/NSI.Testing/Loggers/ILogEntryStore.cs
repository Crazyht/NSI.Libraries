namespace NSI.Testing.Loggers;

/// <summary>
/// Thread-safe in-memory (or pluggable) aggregation surface for captured <see cref="LogEntry"/>
/// instances produced by test <c>ILogger</c> implementations (e.g. <c>MockLogger&lt;T&gt;</c>).
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Capture chronological sequence of log messages and scope lifecycle events.</description></item>
///   <item><description>Provide immutable snapshot enumeration for deterministic test assertions and
///     benchmark analysis.</description></item>
///   <item><description>Support isolation by clearing between tests while preventing cross‑test bleed.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Implementations MUST guarantee insertion order preservation (no reordering).</description></item>
///   <item><description>Return defensive copies or immutable views to callers (avoid exposing internals).</description></item>
///   <item><description>Use lightweight synchronization (e.g. lock-free structures or fine-grained
///     locking) to minimize contention under parallel test execution.</description></item>
///   <item><description>Ensure <see cref="Clear"/> cannot interleave partially with <see cref="Add"/> (atomic
///     semantics).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Typical entry volume per test is low (hundreds); allocate with growth strategy
///     to minimize resize churn.</description></item>
///   <item><description><see cref="GetAll"/> should be O(n) with minimal copying; prefer reusing immutable
///     snapshot when safe.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: All members MUST be safe for concurrent invocation by multiple logger
/// instances operating in parallel test scenarios.</para>
/// </remarks>
/// <example>
/// <code>
/// // Injection in test composition
/// ILogEntryStore store = new InMemoryLogEntryStore();
/// var logger = new MockLogger&lt;MyComponent&gt;(store);
///
/// // Exercise component producing logs
/// component.Execute();
///
/// // Assert on captured entries
/// var errors = store.GetAll()
///   .LogsOnly()
///   .WithLogLevel(LogLevel.Error)
///   .ToList();
/// Assert.Single(errors);
///
/// // Reset between tests
/// store.Clear();
/// </code>
/// </example>
/// <seealso cref="LogEntry"/>
/// <seealso cref="EntryType"/>
public interface ILogEntryStore {
  /// <summary>Adds a log or scope entry to the store preserving chronological order.</summary>
  /// <param name="entry">Fully constructed immutable log entry (non-null).</param>
  /// <exception cref="ArgumentNullException">When <paramref name="entry"/> is null.</exception>
  /// <remarks>
  /// <para>Implementations MUST ensure visibility of the inserted entry to subsequent
  /// <see cref="GetAll"/> calls that occur after this method returns.</para>
  /// </remarks>
  public void Add(LogEntry entry);

  /// <summary>Retrieves all entries in insertion (chronological) order.</summary>
  /// <returns>Read-only snapshot list of captured entries (never null, possibly empty).</returns>
  /// <remarks>
  /// <para>The returned collection MUST NOT reflect future additions; callers rely on snapshot
  /// semantics to produce deterministic assertions.</para>
  /// <para>Implementations may return an internally cached immutable list if no intervening
  /// mutations occurred since the previous call.</para>
  /// </remarks>
  public IReadOnlyList<LogEntry> GetAll();

  /// <summary>Clears all stored entries atomically.</summary>
  /// <remarks>
  /// <para>Subsequent calls to <see cref="GetAll"/> MUST return an empty collection until new
  /// entries are added.</para>
  /// <para>If invoked concurrently with <see cref="Add"/>, implementation MUST guarantee either
  /// the entry appears in a later snapshot or is fully discarded (no partial/corrupt state).</para>
  /// </remarks>
  public void Clear();
}
