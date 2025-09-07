using Microsoft.EntityFrameworkCore;

namespace NSI.EntityFramework;

/// <summary>
/// Defines an interface for seeding data into a database context.
/// </summary>
public interface IDataSeeder {
  /// <summary>
  /// Checks if the data seeder needs to run based on the current state of the database.
  /// </summary>
  /// <param name="dbContext">The database context to check.</param>
  /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether seeding is required.</returns>
  public Task<bool> CheckIfNeedToRunAsync(DbContext dbContext, CancellationToken stoppingToken);

  /// <summary>
  /// Seeds data into the specified database context.
  /// </summary>
  /// <param name="dbContext">The database context to seed data into.</param>
  /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
  /// <returns>A task that represents the asynchronous seeding operation.</returns>
  public Task SeedAsync(DbContext dbContext, CancellationToken stoppingToken);
}
