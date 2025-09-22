using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers;

/// <summary>
/// Test ILoggerFactory capturing logs into a shared <see cref="ILogEntryStore"/> with filtering.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Create category loggers that record <see cref="LogEntry"/> instances.</description></item>
///   <item><description>Apply global and per-category minimum level filtering.</description></item>
///   <item><description>Optionally capture scope start / end boundaries for hierarchy analysis.</description></item>
///   <item><description>Provide deterministic, thread-safe logger retrieval (one instance per category).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Reuse a single factory + store per test fixture to aggregate all logs.</description></item>
///   <item><description>Call <c>Dispose()</c> at fixture teardown to release cached loggers.</description></item>
///   <item><description>Use <see cref="MockLoggerOptions.CategoryLevels"/> for noisy categories instead of
///     ad-hoc filtering in assertions.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Logger retrieval is O(1) amortized with dictionary key lookup.</description></item>
///   <item><description>Filtering is branch + numeric comparison (no allocations).</description></item>
///   <item><description>Parent scope resolution avoids LINQ (<c>Skip</c>/<c>First</c>) to eliminate iterator
///     allocations in hot paths.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Logger cache protected by a lock; per-logger scope stacks are isolated via
/// <see cref="AsyncLocal{T}"/>.</para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryLogEntryStore();
/// using var factory = new MockLoggerFactory(store, new MockLoggerOptions {
///   MinimumLevel = LogLevel.Debug,
///   CaptureScopes = true
/// });
/// var logger = factory.CreateLogger("Test");
/// using (logger.BeginScope(new Dictionary&lt;string, object&gt; { ["UserId"] = 7 })) {
///   logger.LogInformation("Processing started");
/// }
/// var infoCount = store.GetAll().LogsOnly().WithLogLevel(LogLevel.Information).Count();
/// </code>
/// </example>
public sealed class MockLoggerFactory(ILogEntryStore store, MockLoggerOptions? options = null): ILoggerFactory {
  private readonly ILogEntryStore _Store = store ?? throw new ArgumentNullException(nameof(store));
  private readonly MockLoggerOptions _Options = options ?? new MockLoggerOptions();
  private readonly Dictionary<string, ILogger> _Loggers = [];
  private readonly Lock _Lock = new();
  private bool _Disposed;

  /// <summary>Creates (or retrieves cached) logger for a category.</summary>
  /// <param name="categoryName">Logger category name (non-null).</param>
  /// <returns>Cached or newly created logger.</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="categoryName"/> is null.</exception>
  /// <exception cref="ObjectDisposedException">Factory disposed.</exception>
  public ILogger CreateLogger(string categoryName) {
    ArgumentNullException.ThrowIfNull(categoryName);
    ThrowIfDisposed();

    lock (_Lock) {
      if (_Loggers.TryGetValue(categoryName, out var existing)) {
        return existing;
      }
      var logger = new MockLoggerWithFiltering(categoryName, _Store, _Options);
      _Loggers[categoryName] = logger;
      return logger;
    }
  }

  /// <summary>No-op (provider model not used in this test implementation).</summary>
  /// <param name="provider">Ignored provider instance.</param>
  public void AddProvider(ILoggerProvider provider) {
    _ = provider; // Intentionally ignored
    _ = _Disposed; // Keep instance semantics
  }

  /// <summary>Disposes cached loggers; underlying store is not disposed.</summary>
  public void Dispose() {
    if (_Disposed) {
      return;
    }
    lock (_Lock) {
      if (_Disposed) {
        return;
      }
      foreach (var logger in _Loggers.Values) {
        if (logger is IDisposable d) {
          d.Dispose();
        }
      }
      _Loggers.Clear();
      _Disposed = true;
    }
  }

  private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_Disposed, nameof(MockLoggerFactory));
}

/// <summary>Configuration options for <see cref="MockLoggerFactory"/>.</summary>
/// <remarks>
/// <para>Set <see cref="MinimumLevel"/> for global cutoff; override per category with
/// <see cref="CategoryLevels"/>; disable scope capture via <see cref="CaptureScopes"/> to reduce
/// noise or allocations.</para>
/// </remarks>
public class MockLoggerOptions {
  /// <summary>Global minimum log level (defaults to Trace = capture all).</summary>
  public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

  /// <summary>Per-category overrides (key = category name, value = minimum level).</summary>
  public Dictionary<string, LogLevel> CategoryLevels { get; } = [];

  /// <summary>When true, scope start/end entries are recorded.</summary>
  public bool CaptureScopes { get; set; } = true;
}

/// <summary>Internal logger applying configured filtering and optional scope capture.</summary>
internal sealed class MockLoggerWithFiltering(
  string categoryName,
  ILogEntryStore store,
  MockLoggerOptions options): ILogger {
  private readonly string _CategoryName = categoryName;
  private readonly ILogEntryStore _Store = store;
  private readonly MockLoggerOptions _Options = options;
  private readonly AsyncLocal<Stack<Guid>> _ScopeStack = new() { Value = new Stack<Guid>() };

  public IDisposable BeginScope<TState>(TState state) where TState : notnull {
    ArgumentNullException.ThrowIfNull(state);
    if (!_Options.CaptureScopes) {
      return new NoOpDisposable();
    }

    var parentId = _ScopeStack.Value!.Count > 0 ? _ScopeStack.Value.Peek() : (Guid?)null;
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
    return new ScopeTracker(_Store, _ScopeStack, newScopeId, _Options.CaptureScopes);
  }

  public bool IsEnabled(LogLevel logLevel) {
    if (_Options.CategoryLevels.TryGetValue(_CategoryName, out var categoryLevel)) {
      return logLevel >= categoryLevel;
    }
    return logLevel >= _Options.MinimumLevel;
  }

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter) {
    ArgumentNullException.ThrowIfNull(formatter);
    if (!IsEnabled(logLevel)) {
      return;
    }

    var message = formatter(state, exception);
    var stateArray = state is object[] arr ? arr : state is not null ? [state] : [];

    Guid? currentScope = null;
    Guid? parentScope = null;
    var stack = _ScopeStack.Value;
    if (stack is { Count: > 0 }) {
      // Enumerate up to two frames without LINQ (top=current, next=parent)
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

  private sealed class ScopeTracker(
    ILogEntryStore store,
    AsyncLocal<Stack<Guid>> stack,
    Guid scopeId,
    bool captureScopes): IDisposable {
    private readonly ILogEntryStore _Store = store;
    private readonly AsyncLocal<Stack<Guid>> _Stack = stack;
    private readonly Guid _ScopeId = scopeId;
    private readonly bool _CaptureScopes = captureScopes;
    private bool _Disposed;

    public void Dispose() {
      if (_Disposed) {
        return;
      }
      _Disposed = true;
      if (_Stack.Value!.TryPop(out var popped) && popped == _ScopeId && _CaptureScopes) {
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

  private sealed class NoOpDisposable: IDisposable { public void Dispose() { } }
}
