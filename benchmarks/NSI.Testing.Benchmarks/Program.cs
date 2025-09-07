using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;

namespace NSI.Testing.Benchmarks {
  /// <summary>
  /// Entry point for MockLogger performance benchmarks.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This program runs comprehensive performance benchmarks for the MockLogger system
  /// to validate performance characteristics and identify optimization opportunities.
  /// </para>
  /// <para>
  /// The benchmarks cover:
  /// <list type="bullet">
  ///   <item><description>Basic logging operations at scale</description></item>
  ///   <item><description>Scope management performance</description></item>
  ///   <item><description>LINQ query performance on large datasets</description></item>
  ///   <item><description>Filtering impact on performance</description></item>
  ///   <item><description>Concurrent access scenarios</description></item>
  ///   <item><description>Memory allocation patterns</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  internal static class Program {
    /// <summary>
    /// Main entry point for benchmark execution.
    /// </summary>
    /// <param name="args">Command line arguments for BenchmarkDotNet.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    public static int Main(string[] args) =>
      BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(args, GetBenchmarkConfig())
        .Any(result => !result.HasCriticalValidationErrors) ? 0 : 1;

    /// <summary>
    /// Gets the benchmark configuration with custom settings.
    /// </summary>
    /// <returns>Configured benchmark setup for optimal measurement accuracy.</returns>
    private static IConfig GetBenchmarkConfig() =>
      DefaultConfig.Instance
        .AddJob(Job.Default
          .WithWarmupCount(3)
          .WithIterationCount(10)
          .WithInvocationCount(1)
          .WithUnrollFactor(1))
        .AddDiagnoser(MemoryDiagnoser.Default)
        .AddDiagnoser(ThreadingDiagnoser.Default)
        .AddExporter(MarkdownExporter.GitHub)
        .AddExporter(HtmlExporter.Default)
        .AddExporter(JsonExporter.Full)
        .WithOptions(ConfigOptions.DisableOptimizationsValidator);
  }
}
