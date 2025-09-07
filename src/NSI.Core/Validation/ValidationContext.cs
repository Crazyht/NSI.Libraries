using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation {
  /// <summary>
  /// Default implementation of <see cref="IValidationContext"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides a concrete implementation of the validation context
  /// that can be used during validation operations. It stores a service provider
  /// for dependency resolution and a dictionary for additional contextual data.
  /// </para>
  /// <para>
  /// The context can be created with a specific service provider or as an empty
  /// context with no service resolution capabilities using the <see cref="Empty"/>
  /// factory method.
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// // Create with DI container
  /// var context = new ValidationContext(serviceProvider);
  /// context.Items["CurrentUser"] = currentUser;
  /// 
  /// // Create empty context for simple validations
  /// var emptyContext = ValidationContext.Empty();
  /// </code>
  /// </para>
  /// </remarks>
  public sealed class ValidationContext: IValidationContext {
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc/>
    public IDictionary<string, object?> Items { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationContext"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> is null.
    /// </exception>
    public ValidationContext(IServiceProvider serviceProvider) {
      ArgumentNullException.ThrowIfNull(serviceProvider);

      ServiceProvider = serviceProvider;
      Items = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Creates an empty validation context with no service provider.
    /// </summary>
    /// <returns>A new empty validation context.</returns>
    /// <remarks>
    /// This factory method creates a validation context with a non-functional service 
    /// provider that always returns null when asked for services. It's useful for 
    /// validation scenarios where no dependencies need to be resolved.
    /// </remarks>
    public static ValidationContext Empty() => new(EmptyServiceProvider.Instance);

    /// <summary>
    /// A service provider implementation that always returns null.
    /// </summary>
    /// <remarks>
    /// Used by the <see cref="Empty"/> method to create contexts without dependency resolution.
    /// </remarks>
    private sealed class EmptyServiceProvider: IServiceProvider {
      /// <summary>
      /// Gets the singleton instance of the <see cref="EmptyServiceProvider"/>.
      /// </summary>
      public static readonly EmptyServiceProvider Instance = new();

      /// <inheritdoc/>
      public object? GetService(Type serviceType) => null;
    }
  }
}
