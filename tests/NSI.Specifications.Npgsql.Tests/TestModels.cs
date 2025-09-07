using Microsoft.EntityFrameworkCore;

namespace NSI.Specifications.Npgsql.Tests;

/// <summary>
/// Simple entity used for integration test verification.
/// </summary>
public sealed class SampleEntity {
  /// <summary>Primary key.</summary>
  public int Id { get; set; }
  /// <summary>Name value to test filtering patterns.</summary>
  public string? Name { get; set; }
}

/// <summary>
/// EF Core context used for integration tests.
/// </summary>
/// <param name="options">Context options.</param>
public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options): DbContext(options) {
  /// <summary>Entity set under test.</summary>
  public DbSet<SampleEntity> Entities => Set<SampleEntity>();
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    ArgumentNullException.ThrowIfNull(modelBuilder);
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<SampleEntity>().ToTable("sample_entities");
  }
}
