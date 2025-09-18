namespace NSI.Core.Validation.Abstractions;

/// <summary>
/// Orchestrates synchronous and asynchronous validation for instances of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Domain object type being validated.</typeparam>
/// <remarks>
/// <para>
/// A validator aggregates one or more <see cref="IValidationRule{T}"/> /
/// <see cref="IAsyncValidationRule{T}"/> implementations and produces an
/// <see cref="IValidationResult"/> describing success or structured failures
/// (<see cref="IValidationError"/> items).
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description><c>Validate</c> must perform only CPU-bound or trivially fast checks.</description></item>
///   <item><description><c>ValidateAsync</c> supports I/O (DB, HTTP, cache) and cancellation.</description></item>
///   <item><description>Both methods return non-null <see cref="IValidationResult"/> (never <see langword="null"/>).</description></item>
///   <item><description>An empty error collection implies success (<c>IsValid == true</c>).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Fail fast: stop expensive rule evaluation after a terminal error if policy allows.</description></item>
///   <item><description>Reuse rule instances (immutability / stateless preferred).</description></item>
///   <item><description>Minimize allocations in the success path (cache empty result).</description></item>
///   <item><description>Provide deterministic ordering of errors (stable rule order).</description></item>
///   <item><description>Use explicit error codes (UPPER_SNAKE_CASE) for client handling.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Implementations should be thread-safe for concurrent validations when they are
/// stateless or immutable. Shared mutable caches must be synchronized.
/// </para>
/// <para>
/// Performance: Prefer batching external calls inside <c>ValidateAsync</c> to reduce round trips;
/// avoid creating interim lists when streaming errors is sufficient.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var validator = new UserValidator();
/// // Synchronous path (pure business rules)
/// var syncResult = validator.Validate(user);
/// if (!syncResult.IsValid) {
///   foreach (var e in syncResult.Errors) {
///     Console.WriteLine($"{e.PropertyName}: {e.ErrorMessage} ({e.ErrorCode})");
///   }
/// }
///
/// // Asynchronous path (DB lookups, uniqueness checks)
/// var asyncResult = await validator.ValidateAsync(user, context, ct);
/// if (!asyncResult.IsValid) {
///   // Map to ProblemDetails or domain exception.
/// }
/// </code>
/// </example>
public interface IValidator<T> {
  /// <summary>
  /// Validates the specified <paramref name="instance"/> synchronously (CPU-bound rules only).
  /// </summary>
  /// <param name="instance">Object instance to validate (never null).</param>
  /// <param name="context">Optional validation context; implementation may create a default when null.</param>
  /// <returns>
  /// Non-null <see cref="IValidationResult"/> whose <see cref="IValidationResult.Errors"/> is empty on success.
  /// </returns>
  /// <remarks>
  /// Use when rule set is purely synchronous. For I/O or cancellable operations prefer
  /// <see cref="ValidateAsync"/>.
  /// </remarks>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
  public IValidationResult Validate(T instance, IValidationContext? context = null);

  /// <summary>
  /// Asynchronously validates the specified <paramref name="instance"/> (supports I/O &amp; cancellation).
  /// </summary>
  /// <param name="instance">Object instance to validate (never null).</param>
  /// <param name="context">Optional validation context; implementation may create a default when null.</param>
  /// <param name="cancellationToken">Token to observe for cancellation.</param>
  /// <returns>
  /// Task resolving to a non-null <see cref="IValidationResult"/> (empty error list on success).
  /// </returns>
  /// <remarks>
  /// Aggregates both synchronous and asynchronous rules. Implementations should:
  /// <list type="bullet">
  ///   <item><description>Honor <paramref name="cancellationToken"/> early.</description></item>
  ///   <item><description>Propagate <see cref="OperationCanceledException"/> immediately.</description></item>
  ///   <item><description>Batch external service calls where feasible.</description></item>
  /// </list>
  /// </remarks>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
  /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
  public Task<IValidationResult> ValidateAsync(
    T instance,
    IValidationContext? context = null,
    CancellationToken cancellationToken = default);
}
