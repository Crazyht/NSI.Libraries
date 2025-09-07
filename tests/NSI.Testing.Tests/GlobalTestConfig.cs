// Global assembly configuration for tests
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 4)]

namespace NSI.Testing.Tests {
  /// <summary>
  /// Global configuration and shared test utilities.
  /// </summary>
  internal static class GlobalTestConfig {

    /// <summary>
    /// Standard timeout for async operations in tests.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Performance test timeout for longer operations.
    /// </summary>
    public static readonly TimeSpan PerformanceTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Performance benchmarks and thresholds.
    /// </summary>
    internal static class PerformanceBenchmarks {
      public const int HighVolumeEntryCount = 10000;
      public const int HighVolumeTimeoutMs = 2000;
      public const int ConcurrentThreadCount = 100;
      public const int ConcurrentTimeoutMs = 3000;
      public const int LinqQueryTimeoutMs = 500;
      public const int ComplexAnalysisTimeoutMs = 1000;
      public const int MemoryPerEntryBytes = 1024;
      public const int MaxMemoryUsageMB = 25;
    }
  }

  /// <summary>
  /// Collection fixture for integration tests that need shared setup.
  /// </summary>
  internal sealed class IntegrationTestCollection: ICollectionFixture<IntegrationTestFixture> {
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
  }
}
