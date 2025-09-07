namespace NSI.Testing.Loggers {
  /// <summary>
  /// Specifies the type of log entry.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This enumeration is used to distinguish between different types of entries
  /// in the log store, allowing for proper categorization and filtering of
  /// log messages and scope operations.
  /// </para>
  /// </remarks>
  public enum EntryType {
    /// <summary>
    /// Represents a standard log message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Log entries contain actual logging information including level, message,
    /// exception details, and structured logging state. These entries represent
    /// the primary output from application logging operations.
    /// </para>
    /// </remarks>
    Log,

    /// <summary>
    /// Represents the beginning of a logging scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope start entries mark the beginning of a logical operation scope
    /// and contain the scope variables and hierarchy information. The scope
    /// identifier and parent relationships are established at this point.
    /// </para>
    /// </remarks>
    ScopeStart,

    /// <summary>
    /// Represents the end of a logging scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope end entries mark the completion of a logical operation scope.
    /// These entries are automatically generated when the scope's IDisposable
    /// is disposed, completing the scope lifecycle.
    /// </para>
    /// </remarks>
    ScopeEnd
  }
}
