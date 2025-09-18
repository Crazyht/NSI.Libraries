using Microsoft.EntityFrameworkCore;

namespace NSI.EntityFramework;

/// <summary>
/// Defines the contract for conditional, idempotent database seed operations.
/// </summary>
/// <remarks>
/// <para>
/// A data seeder coordinates initial or supplemental domain data population (reference / lookup
/// tables, system accounts, baseline configuration) without duplicating existing records. The
/// seeding process is split into an inexpensive pre-check (<see cref="CheckIfNeedToRunAsync"/>) and
/// the execution phase (<see cref="SeedAsync"/>).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><see cref="CheckIfNeedToRunAsync"/> must avoid heavy queries (metadata / count / EXISTS).</description></item>
///   <item><description><see cref="SeedAsync"/> must be idempotent (safe to re-run).</description></item>
///   <item><description>No assumptions about transaction boundaries (caller decides).</description></item>
///   <item><description>Should not perform destructive operations (no truncation / mass delete).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Group related inserts in minimal batches to reduce round‑trips.</description></item>
///   <item><description>Prefer deterministic keys (strongly-typed ids) for stable re-runs.</description></item>
///   <item><description>Use cancellation token early and often in async calls.</description></item>
///   <item><description>Log only high-level events (started / completed / skipped).</description></item>
/// </list>
/// </para>
/// <para>Performance: Pre-check expected to be O(1) index seek / EXISTS. Seeding phase should bulk
/// insert when possible to minimize overhead.</para>
/// <para>Thread-safety: Implementations are not required to be thread-safe; callers should serialize
/// seeding orchestration.</para>
/// </remarks>
public interface IDataSeeder {
  /// <summary>
  /// Determines whether the seeding operation is required for the current database state.
  /// </summary>
  /// <param name="dbContext">Target <see cref="DbContext"/> (non-null).</param>
  /// <param name="stoppingToken">Cancellation token to observe.</param>
  /// <returns>
  /// A task producing <c>true</c> if seeding should run; otherwise <c>false</c> to skip execution.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
  /// <exception cref="OperationCanceledException">If the operation is canceled.</exception>
  public Task<bool> CheckIfNeedToRunAsync(DbContext dbContext, CancellationToken stoppingToken);

  /// <summary>
  /// Executes the seeding operation (idempotent) against the provided context.
  /// </summary>
  /// <param name="dbContext">Target <see cref="DbContext"/> (non-null).</param>
  /// <param name="stoppingToken">Cancellation token to observe.</param>
  /// <returns>Task that completes when seeding logic has finished (successfully or via exception).</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
  /// <exception cref="OperationCanceledException">If the operation is canceled.</exception>
  public Task SeedAsync(DbContext dbContext, CancellationToken stoppingToken);
}
