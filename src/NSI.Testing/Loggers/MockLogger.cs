using Microsoft.Extensions.Logging;

namespace NSI.Testing.Loggers {

  /// <summary>
  /// Provides a mock implementation of <see cref="ILogger{T}"/> for testing purposes.
  /// </summary>
  /// <typeparam name="T">The type whose name is used for the logger category name.</typeparam>
  /// <remarks>
  /// <para>
  /// This mock logger captures all logging operations including scope management
  /// and stores them in a centralized store for test verification. It supports
  /// hierarchical scopes and maintains parent-child relationships between scopes,
  /// enabling comprehensive testing of logging behavior in complex scenarios.
  /// </para>
  /// <para>
  /// The logger is fully thread-safe and uses <see cref="AsyncLocal{T}"/> to maintain
  /// scope context across asynchronous operations. This ensures that logging calls
  /// made within async methods are properly associated with their originating scopes.
  /// </para>
  /// <para>
  /// Key features:
  /// <list type="bullet">
  ///   <item><description>Complete ILogger{T} implementation for drop-in replacement</description></item>
  ///   <item><description>Hierarchical scope tracking with parent-child relationships</description></item>
  ///   <item><description>Thread-safe operations for parallel test execution</description></item>
  ///   <item><description>AsyncLocal scope context for async/await scenarios</description></item>
  ///   <item><description>Centralized storage for cross-logger test verification</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Usage in dependency injection:
  /// <code>
  /// services.AddSingleton&lt;ILogEntryStore, InMemoryLogEntryStore&gt;();
  /// services.AddSingleton(typeof(ILogger&lt;&gt;), typeof(MockLogger&lt;&gt;));
  /// </code>
  /// </para>
  /// </remarks>
  /// <remarks>
  /// Initializes a new instance of the <see cref="MockLogger{T}"/> class.
  /// </remarks>
  /// <param name="store">The store where log entries will be saved.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="store"/> is <see langword="null"/>.
  /// </exception>
  /// <remarks>
  /// <para>
  /// The logger requires a store implementation to capture log entries.
  /// The same store instance can be shared across multiple logger instances
  /// to enable centralized log collection and analysis in tests.
  /// </para>
  /// <para>
  /// Each logger instance maintains its own scope stack using AsyncLocal,
  /// ensuring that scope contexts are properly isolated between different
  /// execution flows while sharing the same underlying storage.
  /// </para>
  /// </remarks>
  public sealed class MockLogger<T>(ILogEntryStore store): ILogger<T> {
    private readonly ILogEntryStore _Store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly AsyncLocal<Stack<Guid>> _ScopeStack = new();

    /// <summary>
    /// Begins a logical operation scope with the specified state.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="state">The state object that defines the scope.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that ends the scope when disposed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method creates a new logging scope and records a scope start entry
    /// in the store. The scope is automatically assigned a unique identifier
    /// and linked to its parent scope if one exists.
    /// </para>
    /// <para>
    /// Scope variables are extracted from the state object. If the state implements
    /// IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; (such as structured logging
    /// states), the key-value pairs are stored as scope variables. Otherwise,
    /// the entire state object is stored as a single variable.
    /// </para>
    /// <para>
    /// The returned IDisposable must be disposed to properly end the scope.
    /// Using a using statement is recommended to ensure proper scope lifecycle:
    /// <code>
    /// using (logger.BeginScope("Operation {OperationId}", operationId)) {
    ///   // Logging within scope
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public IDisposable BeginScope<TState>(TState state)
      where TState : notnull {
      ArgumentNullException.ThrowIfNull(state);

      // Ensure the AsyncLocal stack is initialized for this context
      _ScopeStack.Value ??= new Stack<Guid>();

      // Determine parent scope
      var parentId = _ScopeStack.Value.Count > 0 ? _ScopeStack.Value.Peek() : (Guid?)null;
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
      _ScopeStack.Value.Push(newScopeId);
      return new ScopeTracker(_Store, _ScopeStack, newScopeId);
    }

    /// <summary>
    /// Checks if the given log level is enabled.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>
    /// Always returns <see langword="true"/> for testing purposes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This mock implementation always returns true to ensure all log messages
    /// are captured regardless of their level. This behavior is appropriate for
    /// testing scenarios where comprehensive log capture is desired.
    /// </para>
    /// <para>
    /// In production scenarios, this method would typically check against
    /// configured minimum log levels, but for testing purposes, capturing
    /// all log levels provides maximum visibility into application behavior.
    /// </para>
    /// </remarks>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <summary>
    /// Writes a log entry with the specified parameters.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="state">The state object containing log data.</param>
    /// <param name="exception">The exception to log, or <see langword="null"/> if none.</param>
    /// <param name="formatter">A function that creates a log message from the state and exception.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="formatter"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method captures the complete logging context including the current scope
    /// hierarchy. The log entry is associated with the current scope (if any) and
    /// maintains the parent-child relationships for proper hierarchical analysis.
    /// </para>
    /// <para>
    /// The state object is stored as an array to accommodate both simple values
    /// and structured logging scenarios. For structured logging states that implement
    /// IEnumerable, the individual elements are preserved for detailed analysis.
    /// </para>
    /// <para>
    /// The formatter function is called to generate the final log message, which
    /// is stored alongside the raw state data for comprehensive test verification.
    /// </para>
    /// </remarks>
    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter) {
      ArgumentNullException.ThrowIfNull(formatter);

      var message = formatter(state, exception);
      var stateArray = state is object[] arr
        ? arr
        : state is not null
          ? [state]
          : [];

      // Get current scope information
      var currentScope = _ScopeStack.Value?.Count > 0
        ? _ScopeStack.Value.Peek()
        : (Guid?)null;
      var parentScope = _ScopeStack.Value?.Count > 1
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

    /// <summary>
    /// Manages the lifecycle of a logging scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This internal class handles the proper cleanup of logging scopes when
    /// they are disposed. It ensures that scope end entries are recorded
    /// and the scope stack is properly maintained.
    /// </para>
    /// <para>
    /// The class implements the disposable pattern safely, preventing multiple
    /// disposals from causing issues and ensuring consistent scope tracking
    /// even in error scenarios.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ScopeTracker"/> class.
    /// </remarks>
    /// <param name="store">The log entry store.</param>
    /// <param name="stack">The scope stack.</param>
    /// <param name="scopeId">The identifier of the scope to track.</param>
    /// <remarks>
    /// <para>
    /// The scope tracker maintains references to the store and stack to properly
    /// manage scope lifecycle. The scope identifier is used to ensure correct
    /// scope end recording even in nested scope scenarios.
    /// </para>
    /// </remarks>
    private sealed class ScopeTracker(ILogEntryStore store, AsyncLocal<Stack<Guid>> stack, Guid scopeId): IDisposable {
      private readonly ILogEntryStore _Store = store;
      private readonly AsyncLocal<Stack<Guid>> _Stack = stack;
      private readonly Guid _ScopeId = scopeId;
      private bool _Disposed;

      /// <summary>
      /// Ends the scope and records the scope end entry.
      /// </summary>
      /// <remarks>
      /// <para>
      /// This method safely ends the scope by removing it from the scope stack
      /// and recording a scope end entry in the store. It handles multiple disposal
      /// calls gracefully and ensures consistent state even if the scope stack
      /// has been modified unexpectedly.
      /// </para>
      /// <para>
      /// The method uses a defensive approach, only popping from the stack if
      /// the expected scope identifier matches the top of the stack, preventing
      /// corruption in complex nested scenarios.
      /// </para>
      /// </remarks>
      public void Dispose() {
        if (_Disposed) {
          return;
        }

        _Disposed = true;

        // Pop from stack and record scope end
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

}
