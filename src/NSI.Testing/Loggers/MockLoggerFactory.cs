using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers;

/// <summary>
/// Provides a mock implementation of <see cref="ILoggerFactory"/> for testing purposes.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates MockLogger instances and provides seamless integration
/// with Microsoft.Extensions.Logging infrastructure. It supports configuration-based
/// log level filtering and can be used as a drop-in replacement for standard
/// logger factories in test scenarios.
/// </para>
/// <para>
/// The factory maintains a centralized log entry store that is shared across
/// all logger instances it creates, enabling comprehensive test analysis
/// across multiple logger categories.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Full ILoggerFactory implementation</description></item>
///   <item><description>Configurable log level filtering</description></item>
///   <item><description>Centralized log entry storage</description></item>
///   <item><description>Thread-safe logger creation</description></item>
///   <item><description>Integration with AddLogging() extension</description></item>
/// </list>
/// </para>
/// <para>
/// Usage with dependency injection:
/// <code>
/// services.AddSingleton&lt;ILoggerFactory&gt;(provider => {
///   var store = provider.GetRequiredService&lt;ILogEntryStore&gt;();
///   return new MockLoggerFactory(store);
/// });
/// </code>
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="MockLoggerFactory"/> class.
/// </remarks>
/// <param name="store">The store where log entries will be saved.</param>
/// <param name="options">The configuration options for the factory.</param>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="store"/> is <see langword="null"/>.
/// </exception>
/// <remarks>
/// <para>
/// Creates a new factory with the specified store and options. If no options
/// are provided, default options are used which capture all log levels.
/// </para>
/// </remarks>
public sealed class MockLoggerFactory(ILogEntryStore store, MockLoggerOptions? options = null): ILoggerFactory {
  private readonly ILogEntryStore _Store = store ?? throw new ArgumentNullException(nameof(store));
  private readonly MockLoggerOptions _Options = options ?? new MockLoggerOptions();
  private readonly Dictionary<string, ILogger> _Loggers = [];
  private readonly Lock _Lock = new();
  private bool _Disposed;

  /// <summary>
  /// Creates a new <see cref="ILogger"/> instance with the specified category name.
  /// </summary>
  /// <param name="categoryName">The category name for messages produced by the logger.</param>
  /// <returns>A new logger instance.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="categoryName"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// Thrown when the factory has been disposed.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method creates logger instances that share the same underlying store
  /// and configuration. Loggers are cached by category name for performance,
  /// ensuring that multiple requests for the same category return the same instance.
  /// </para>
  /// </remarks>
  public ILogger CreateLogger(string categoryName) {
    ArgumentNullException.ThrowIfNull(categoryName);
    ThrowIfDisposed();

    lock (_Lock) {
      if (_Loggers.TryGetValue(categoryName, out var existingLogger)) {
        return existingLogger;
      }

      var logger = new MockLoggerWithFiltering(categoryName, _Store, _Options);
      _Loggers[categoryName] = logger;
      return logger;
    }
  }

  /// <summary>
  /// Adds an <see cref="ILoggerProvider"/> to the factory.
  /// </summary>
  /// <param name="provider">The provider to add.</param>
  /// <remarks>
  /// <para>
  /// This method is provided for interface compatibility but does nothing
  /// in the mock implementation since all loggers are created directly
  /// by the factory.
  /// </para>
  /// </remarks>
  public void AddProvider(ILoggerProvider provider) {
    // MockLoggerFactory doesn't use providers - all loggers are created directly
    // This method is provided for interface compatibility
    // Marquer explicitement l'utilisation pour éviter IDE0060 / CA1822 (warnings => errors)
    _ = provider; // Intentionnellement ignoré
    _ = _Disposed; // Reference à un champ d'instance pour éviter suggestion static
  }

  /// <summary>
  /// Performs application-defined tasks associated with freeing, releasing,
  /// or resetting unmanaged resources.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Disposes all created loggers and prevents further logger creation.
  /// The underlying store is not disposed as it may be shared with other
  /// components.
  /// </para>
  /// </remarks>
  public void Dispose() {
    if (_Disposed) {
      return;
    }

    lock (_Lock) {
      if (_Disposed) {
        return;
      }

      foreach (var logger in _Loggers.Values) {
        if (logger is IDisposable disposable) {
          disposable.Dispose();
        }
      }

      _Loggers.Clear();
      _Disposed = true;
    }
  }

  private void ThrowIfDisposed() =>
    ObjectDisposedException.ThrowIf(_Disposed, nameof(MockLoggerFactory));
}

/// <summary>
/// Configuration options for <see cref="MockLoggerFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of MockLogger instances created by
/// the factory, including log level filtering and other testing-specific
/// configurations.
/// </para>
/// </remarks>
public class MockLoggerOptions {
  /// <summary>
  /// Gets or sets the minimum log level to capture.
  /// </summary>
  /// <value>
  /// The minimum <see cref="LogLevel"/> that will be captured by loggers.
  /// Log entries below this level will be ignored.
  /// </value>
  /// <remarks>
  /// <para>
  /// Setting this to <see cref="LogLevel.None"/> will capture all log levels.
  /// Setting it to higher levels like <see cref="LogLevel.Information"/> will
  /// ignore <see cref="LogLevel.Trace"/> and <see cref="LogLevel.Debug"/> messages.
  /// </para>
  /// </remarks>
  public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

  /// <summary>
  /// Gets or sets the category-specific minimum log levels.
  /// </summary>
  /// <value>
  /// A dictionary mapping category names to their minimum log levels.
  /// </value>
  /// <remarks>
  /// <para>
  /// This allows fine-grained control over log levels for specific categories.
  /// For example, you might want to capture only warnings and errors from
  /// a noisy component while capturing all levels from the component under test.
  /// </para>
  /// <para>
  /// If a category is not found in this dictionary, the <see cref="MinimumLevel"/>
  /// is used as the default.
  /// </para>
  /// </remarks>
  public Dictionary<string, LogLevel> CategoryLevels { get; } = [];

  /// <summary>
  /// Gets or sets a value indicating whether to capture scope operations.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if scope start and end entries should be captured;
  /// otherwise, <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// Setting this to <see langword="false"/> will prevent scope start and end
  /// entries from being stored, reducing noise in tests that don't need to
  /// analyze scope behavior.
  /// </para>
  /// </remarks>
  public bool CaptureScopes { get; set; } = true;
}

/// <summary>
/// Internal logger implementation that supports filtering based on configuration.
/// </summary>
internal sealed class MockLoggerWithFiltering(string categoryName, ILogEntryStore store, MockLoggerOptions options): ILogger {
  private readonly string _CategoryName = categoryName;
  private readonly ILogEntryStore _Store = store;
  private readonly MockLoggerOptions _Options = options;
  private readonly AsyncLocal<Stack<Guid>> _ScopeStack = new() {
    Value = new Stack<Guid>()
  };

  public IDisposable BeginScope<TState>(TState state)
    where TState : notnull {
    ArgumentNullException.ThrowIfNull(state);

    if (!_Options.CaptureScopes) {
      return new NoOpDisposable();
    }

    // Determine parent scope
    var parentId = _ScopeStack.Value?.Count > 0 ? _ScopeStack.Value.Peek() : (Guid?)null;
    var newScopeId = Guid.NewGuid();

    // Extract scope variables
    var variables = state is IEnumerable<KeyValuePair<string, object>> pairs
      ? pairs.Select(p => (object)p).ToArray()
      : [state];

    // Record scope start
    _Store.Add(new LogEntry(
      EntryType.ScopeStart,
      newScopeId,
      parentId,
      level: null,
      eventId: null,
      message: null,
      exception: null,
      state: variables));

    // Push to stack
    _ScopeStack.Value!.Push(newScopeId);
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
    var stateArray = state is object[] arr
      ? arr
      : (state is not null)
        ? [state]
        : [];

    // Get current scope information
    var currentScope = _ScopeStack.Value!.Count > 0
      ? _ScopeStack.Value.Peek()
      : (Guid?)null;
    var parentScope = _ScopeStack.Value.Count > 1
      ? _ScopeStack.Value.Skip(1).First()
      : (Guid?)null;

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

  private sealed class ScopeTracker(ILogEntryStore store, AsyncLocal<Stack<Guid>> stack, Guid scopeId, bool captureScopes): IDisposable {
    private readonly ILogEntryStore _Store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly AsyncLocal<Stack<Guid>> _Stack = stack ?? throw new ArgumentNullException(nameof(stack));
    private readonly Guid _ScopeId = scopeId;
    private readonly bool _CaptureScopes = captureScopes;
    private bool _Disposed;

    public void Dispose() {
      if (_Disposed) {
        return;
      }

      _Disposed = true;

      // Pop from stack
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

  private sealed class NoOpDisposable: IDisposable {
    public void Dispose() {
      // No operation
    }
  }
}
