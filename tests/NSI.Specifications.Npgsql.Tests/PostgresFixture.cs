using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Npgsql.Optimization.Npgsql;

namespace NSI.Specifications.Npgsql.Tests;

/// <summary>
/// Provides a single PostgreSQL container for all tests in the collection.
/// </summary>
public sealed class PostgresFixture: IAsyncLifetime {
  private IContainer? _Container;

  /// <summary>
  /// Gets the configured <see cref="DbContextOptions{TContext}"/> for the sample context.
  /// </summary>
  public DbContextOptions<SampleDbContext> Options { get; private set; } = default!;

  /// <summary>
  /// Starts the PostgreSQL container, creates schema and seeds sample data.
  /// </summary>
  public async Task InitializeAsync() {
    var pgBuilder = new ContainerBuilder()
      .WithImage("postgres:16-alpine")
      .WithCleanUp(true)
      .WithEnvironment("POSTGRES_PASSWORD", "postgres")
      .WithEnvironment("POSTGRES_USER", "postgres")
      .WithEnvironment("POSTGRES_DB", "specs")
      .WithPortBinding(5432, assignRandomHostPort: true)
      .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432));

    _Container = pgBuilder.Build();
    await _Container.StartAsync().ConfigureAwait(false);

    var mappedPort = _Container.GetMappedPublicPort(5432);
    var connectionString = $"Host=localhost;Port={mappedPort};Database=specs;Username=postgres;Password=postgres";
    var optionsBuilder = new DbContextOptionsBuilder<SampleDbContext>()
      .UseNpgsql(connectionString);
    Options = optionsBuilder.Options;

    using var ctx = new SampleDbContext(Options);
    await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
    if (!await ctx.Entities.AnyAsync().ConfigureAwait(false)) {
      ctx.Entities.AddRange(
        new SampleEntity { Name = "Alpha" },
        new SampleEntity { Name = "alphabet" },
        new SampleEntity { Name = "Beta" },
        new SampleEntity { Name = "gamma" }
      );
      await ctx.SaveChangesAsync().ConfigureAwait(false);
    }

    // Register optimizations once
    NpgsqlTextOptimizations.RegisterAll();
  }

  /// <summary>
  /// Stops and disposes the PostgreSQL container.
  /// </summary>
  public async Task DisposeAsync() {
    if (_Container != null) {
      await _Container.StopAsync().ConfigureAwait(false);
      await _Container.DisposeAsync().ConfigureAwait(false);
    }
  }
}

[CollectionDefinition(CollectionName)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit collection naming pattern for clarity.")]
public sealed class PostgresCollection: ICollectionFixture<PostgresFixture> {
  public const string CollectionName = "NpgsqlSpecs";
}
