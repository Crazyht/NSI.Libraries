namespace NSI.Core.Mediator;
/// <summary>
/// Configuration options for the Mediator.
/// </summary>
/// <remarks>
/// <para>
/// These options control various aspects of mediator behavior including
/// performance optimizations, logging levels, and timeout settings.
/// </para>
/// </remarks>
public class MediatorOptions {
  /// <summary>
  /// Gets or sets the default timeout for request processing.
  /// </summary>
  /// <value>The default timeout. Default is 30 seconds.</value>
  public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  /// Gets or sets a value indicating whether to enable detailed logging.
  /// </summary>
  /// <value><c>true</c> to enable detailed logging; otherwise, <c>false</c>. Default is <c>false</c>.</value>
  public bool EnableDetailedLogging { get; set; }

  /// <summary>
  /// Gets or sets the maximum number of concurrent notification handlers.
  /// </summary>
  /// <value>The maximum concurrent handlers. Default is 10.</value>
  public int MaxConcurrentNotificationHandlers { get; set; } = 10;

  /// <summary>
  /// Gets or sets a value indicating whether to validate handler registration at startup.
  /// </summary>
  /// <value><c>true</c> to validate handlers at startup; otherwise, <c>false</c>. Default is <c>true</c>.</value>
  public bool ValidateHandlersAtStartup { get; set; } = true;
}
