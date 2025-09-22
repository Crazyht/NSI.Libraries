namespace NSI.Core.Validation.Abstractions;

/// <summary>
/// Provides ambient contextual services and ad‑hoc data for validation rule execution.
/// </summary>
/// <remarks>
/// <para>
/// Passed to each validation rule so that complex rules can resolve dependencies (repositories,
/// domain services, clock, etc.) and share supplemental data (<see cref="Items"/>).
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Keep the context lightweight (no heavy object graphs).</description></item>
///   <item><description>Prefer resolving scoped services lazily only when actually needed.</description></item>
///   <item><description>Use <see cref="Items"/> for transient per-validation data only.</description></item>
///   <item><description>Do not store large collections or cache results permanently.</description></item>
///   <item><description>Avoid leaking the context outside the validation pipeline.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: A context instance is typically NOT thread-safe and intended for a single
/// validation flow. If parallel rule execution is introduced, either supply independent
/// contexts or ensure external synchronization around shared mutable state (e.g. <see cref="Items"/>).
/// </para>
/// <para>
/// Performance: Accessing <see cref="ServiceProvider"/> is O(1); prefer aggregating required
/// services once instead of repeated resolution inside tight loops.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Resolving a service inside a rule
/// var repo = context.ServiceProvider.GetRequiredService&lt;IUserRepository&gt;();
/// var user = await repo.GetAsync(userId, cancellationToken);
///
/// // Using Items to share data between rules
/// if (!context.Items.TryGetValue("Region", out var regionObj)) {
///   context.Items["Region"] = regionObj = await repo.ResolveRegionAsync(userId, ct);
/// }
/// var region = (Region)regionObj!;
/// </code>
/// </example>
public interface IValidationContext {
  /// <summary>
  /// Gets the underlying service provider used for resolving dependencies during validation.
  /// </summary>
  /// <remarks>
  /// Should expose the application DI scope (scoped lifetime) so that disposable services
  /// resolved during validation follow the same lifetime boundaries as the calling pipeline.
  /// Never returns <see langword="null"/>.
  /// </remarks>
  public IServiceProvider ServiceProvider { get; }

  /// <summary>
  /// Gets a mutable dictionary for passing ad‑hoc contextual data between validation rules.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Keys are case-sensitive. Consumers should check for key presence prior to casting.
  /// </para>
  /// <para>
  /// Values may be <see langword="null"/>. Avoid storing large objects; prefer lightweight
  /// identifiers and re-resolve heavier data through <see cref="ServiceProvider"/> when needed.
  /// </para>
  /// </remarks>
  public IDictionary<string, object?> Items { get; }
}
