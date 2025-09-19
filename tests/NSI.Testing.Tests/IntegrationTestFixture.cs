using NSI.Testing.Loggers;

namespace NSI.Testing.Tests;
/// <summary>
/// Shared fixture for integration tests.
/// Implements the dispose pattern to properly release resources.
/// </summary>
internal sealed class IntegrationTestFixture: IDisposable {
  private bool _Disposed;

  /// <summary>
  /// Gets the shared log entry store.
  /// </summary>
  public ILogEntryStore SharedStore { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="IntegrationTestFixture"/> class.
  /// </summary>
  public IntegrationTestFixture() =>
    SharedStore = new InMemoryLogEntryStore();

  /// <summary>
  /// Releases the resources used by the <see cref="IntegrationTestFixture"/> instance.
  /// </summary>
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Releases the unmanaged resources and optionally the managed resources.
  /// </summary>
  /// <param name="disposing">
  /// True to release both managed and unmanaged resources;
  /// false to release only unmanaged resources.
  /// </param>
  private void Dispose(bool disposing) {
    if (!_Disposed) {
      if (disposing) {
        // Dispose managed resources.
        SharedStore.Clear();
      }

      // Dispose unmanaged resources here if any.
      _Disposed = true;
    }
  }

  /// <summary>
  /// Finalizes the instance.
  /// </summary>
  ~IntegrationTestFixture() {
    Dispose(false);
  }
}
