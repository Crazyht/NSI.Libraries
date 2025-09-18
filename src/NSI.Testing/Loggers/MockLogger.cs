using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers;

/// <summary>
/// Test double for <see cref="ILogger{TCategoryName}"/> capturing structured log entries and scopes.
/// </summary>
/// <typeparam name="T">Category type used for logger name resolution.</typeparam>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Capture every log invocation (all levels always enabled).</description></item>
///   <item><description>Track hierarchical scope lifecycles (start/end) with parent relationships.</description></item>
///   <item><description>Persist immutable <see cref="LogEntry"/> records into an injected store for
///     deterministic assertions.</description></item>
/// </list>
/// </para>
/// <para>Design Notes:
/// <list type="bullet">
///   <item><description>Uses <see cref="AsyncLocal{T}"/> to maintain an ambient scope stack per async
///     flow.</description></item>
///   <item><description>Parent scope detection is O(1) (no LINQ allocations) during logging.</description></item>
///   <item><description>Primary constructor wires required dependencies (DI friendly).</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Logging path allocates only the <see cref="LogEntry"/> and state wrapper array
///     (if required).</description></item>
///   <item><description>Scope push/pop are O(1); parent scope lookup avoids LINQ (<c>Skip/First</c>) to
///     eliminate iterator allocations.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Safe for concurrent usage across test code; each async execution context
/// gets an isolated scope stack while sharing the underlying store.</para>
/// <para>Usage Pattern:</para>
/// </remarks>
/// <example>
/// <code>
/// // DI registration (test composition)
/// services.AddSingleton&lt;ILogEntryStore, InMemoryLogEntryStore&gt;();
/// services.AddSingleton(typeof(ILogger&lt;&gt;), typeof(MockLogger&lt;&gt;));
///
/// // In test
/// var logger = provider.GetRequiredService&lt;ILogger&lt;Sample&gt;&gt;();
/// using (logger.BeginScope(new Dictionary&lt;string, object&gt; { ["UserId"] = 42 })) {
///   logger.LogInformation("Processing {Id}", 123);
/// }
///
/// var entries = store.GetAll();
/// Assert.Single(entries.LogsOnly().WithLogLevel(LogLevel.Information));
/// </code>
/// </example>
/// <seealso cref="ILogEntryStore"/>
/// <seealso cref="LogEntry"/>
public sealed class MockLogger<T>(ILogEntryStore store): ILogger<T> {
  private readonly ILogEntryStore _Store = store ?? throw new ArgumentNullException(nameof(store));
  private readonly AsyncLocal<Stack<Guid>> _ScopeStack = new();

  /// <summary>Begins a new logical scope associated with the calling asynchronous context.</summary>
  /// <typeparam name="TState">State payload type (structured logging template state).</typeparam>
  /// <param name="state">Non-null state value supplying scope variables.</param>
  /// <returns><see cref="IDisposable"/> token that ends the scope upon disposal.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="state"/> is null.</exception>
  /// <remarks>
  /// <para>State extraction rules:
  /// <list type="bullet">
  ///   <item><description>If <paramref name="state"/> implements
  ///     <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> its pairs are
  ///     stored individually.</description></item>
  ///   <item><description>Otherwise the raw state value is stored as a single element.</description></item>
  /// </list>
  /// </para>
  /// <para>Always wrap in a C# <c>using</c> block to guarantee balanced scope end emission.</para>
  /// </remarks>
  public IDisposable BeginScope<TState>(TState state) where TState : notnull {
    ArgumentNullException.ThrowIfNull(state);

    _ScopeStack.Value ??= new Stack<Guid>();
    var parentId = _ScopeStack.Value.Count > 0 ? _ScopeStack.Value.Peek() : (Guid?)null;
    var newScopeId = Guid.NewGuid();

    var variables = state is IEnumerable<KeyValuePair<string, object>> pairs
      ? pairs.Select(p => (object)p).ToArray()
      : [state];

    _Store.Add(new LogEntry(
      EntryType.ScopeStart,
      newScopeId,
      parentId,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state: variables));

    _ScopeStack.Value.Push(newScopeId);
    return new ScopeTracker(_Store, _ScopeStack, newScopeId);
  }

  /// <summary>Indicates whether provided <paramref name="logLevel"/> is enabled (always true).</summary>
  /// <param name="logLevel">Evaluated log level.</param>
  /// <returns>Always <see langword="true"/> ensuring full capture.</returns>
  /// <remarks>All levels enabled to maximize diagnostic visibility in tests.</remarks>
  public bool IsEnabled(LogLevel logLevel) {
    _ = logLevel; // suppress unused warnings; semantic always-on.
    return true;
  }

  /// <summary>Captures a log message with structured state and optional exception.</summary>
  /// <typeparam name="TState">State payload type.</typeparam>
  /// <param name="logLevel">Log severity.</param>
  /// <param name="eventId">Associated event identifier.</param>
  /// <param name="state">State object (template values or arbitrary payload).</param>
  /// <param name="exception">Optional exception instance.</param>
  /// <param name="formatter">Formatter producing human-readable message.</param>
  /// <exception cref="ArgumentNullException">When <paramref name="formatter"/> is null.</exception>
  /// <remarks>
  /// <para>State storage strategy:
  /// <list type="bullet">
  ///   <item><description>If already an <c>object[]</c>, reused directly (zero copy).</description></item>
  ///   <item><description>Else wrapped in a single-element array to keep uniform shape.</description></item>
  /// </list>
  /// </para>
  /// <para>Scope linkage uses current stack top for <see cref="LogEntry.ScopeId"/> and second frame
  /// (if present) for <see cref="LogEntry.ParentScopeId"/> without LINQ allocation.</para>
  /// </remarks>
  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter) {
    ArgumentNullException.ThrowIfNull(formatter);

    var message = formatter(state, exception);
    var stateArray = state is object[] arr ? arr : state is not null ? [state] : [];

    Guid? currentScope = null;
    Guid? parentScope = null;
    var stack = _ScopeStack.Value;
    if (stack is { Count: > 0 }) {
      // Enumerate up to two frames (top=current, next=parent) without LINQ.
      using var e = stack.GetEnumerator();
      if (e.MoveNext()) {
        currentScope = e.Current;
        if (e.MoveNext()) {
          parentScope = e.Current;
        }
      }
    }

    _Store.Add(new LogEntry(
      EntryType.Log,
      currentScope,
      parentScope,
      logLevel,
      eventId,
      message,
      exception,
      stateArray));
  }

  /// <summary>Internal disposable tracking a single scope lifecycle.</summary>
  /// <remarks>
  /// <para>Records a <see cref="EntryType.ScopeEnd"/> upon first disposal; subsequent disposals are
  /// ignored (idempotent).</para>
  /// <para>Stack integrity: only pops if the tracked scope id matches the current top to avoid
  /// corruption in misordered dispose scenarios.</para>
  /// </remarks>
  private sealed class ScopeTracker(
    ILogEntryStore store,
    AsyncLocal<Stack<Guid>> stack,
    Guid scopeId): IDisposable {
    private readonly ILogEntryStore _Store = store;
    private readonly AsyncLocal<Stack<Guid>> _Stack = stack;
    private readonly Guid _ScopeId = scopeId;
    private bool _Disposed;

    /// <summary>Ends the scope (first call only) and emits a scope end entry.</summary>
    public void Dispose() {
      if (_Disposed) {
        return;
      }
      _Disposed = true;

      if (_Stack.Value!.TryPop(out var popped) && popped == _ScopeId) {
        _Store.Add(new LogEntry(
          EntryType.ScopeEnd,
          _ScopeId,
          parentScopeId: null,
          level: null,
          eventId: null,
          message: null,
          exception: null,
          state: []));
      }
    }
  }
}
