namespace NSI.Core.Validation.Abstractions {
  /// <summary>
  /// Provides contextual information during validation operations.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This interface enables validation rules to access dependencies and
  /// additional context data that may be required during validation operations.
  /// </para>
  /// <para>
  /// The context can be used to:
  /// <list type="bullet">
  ///   <item><description>Access services through dependency injection</description></item>
  ///   <item><description>Pass additional data needed for complex validations</description></item>
  ///   <item><description>Share state between multiple validation rules</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public interface IValidationContext {
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    /// <remarks>
    /// Use this to resolve dependencies needed during validation,
    /// such as repositories, domain services, or other utilities.
    /// </remarks>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets additional context data as key-value pairs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stores arbitrary data that may be needed by validation rules.
    /// Common examples include user context, tenant information,
    /// or operation-specific parameters.
    /// </para>
    /// <para>
    /// Values can be null, so consumers should handle null values appropriately.
    /// </para>
    /// </remarks>
    public IDictionary<string, object?> Items { get; }
  }
}
