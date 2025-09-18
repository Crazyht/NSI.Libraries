namespace NSI.Core.Validation.Abstractions;

/// <summary>
/// Defines an asynchronous validation rule that can be applied to a domain object.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
/// <remarks>
/// <para>
/// Each implementation encapsulates a single validation concern (Single Responsibility).
/// Rules are typically composed by a validator that aggregates multiple rule instances
/// and executes them to produce a consolidated set of <see cref="IValidationError"/> values.
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Return errors, do not throw for business validation failures.</description></item>
///   <item><description>Throw only for programmer errors (e.g. null arguments).</description></item>
///   <item><description>Be side-effect free (except benign caching).</description></item>
///   <item><description>Return an empty sequence (never <see langword="null"/>) on success.</description></item>
///   <item><description>Respect cancellation tokens for long operations.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Prefer stateless / immutable implementations so instances can be reused safely.
/// </para>
/// <para>
/// Performance: Avoid unnecessary allocations; stream errors or use small arrays where practical.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class EmailFormatRule: IAsyncValidationRule&lt;User&gt; {
///   private static readonly Regex EmailRegex =
///     new("^[^@\\n]+@[^@\\n]+\\.[^@\\n]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
///
///   public Task&lt;IEnumerable&lt;IValidationError&gt;&gt; ValidateAsync(
///     User instance,
///     IValidationContext context,
///     CancellationToken cancellationToken = default) {
///     ArgumentNullException.ThrowIfNull(instance);
///     ArgumentNullException.ThrowIfNull(context);
///     if (string.IsNullOrWhiteSpace(instance.Email)) {
///       return Task.FromResult&lt;IEnumerable&lt;IValidationError&gt;&gt;(new [] {
///         new ValidationError("Email", "REQUIRED", "Email is required")
///       });
///     }
///     if (!EmailRegex.IsMatch(instance.Email)) {
///       return Task.FromResult&lt;IEnumerable&lt;IValidationError&gt;&gt;(new [] {
///         new ValidationError("Email", "INVALID_FORMAT", "Email format is invalid")
///       });
///     }
///     return Task.FromResult&lt;IEnumerable&lt;IValidationError&gt;&gt;(Array.Empty&lt;IValidationError&gt;());
///   }
/// }
/// </code>
/// </example>
public interface IAsyncValidationRule<T> {
  /// <summary>
  /// Validates the specified <paramref name="instance"/> asynchronously.
  /// </summary>
  /// <param name="instance">The object instance to validate (never null).</param>
  /// <param name="context">Context providing services and ambient data (never null).</param>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>
  /// A task resolving to a (possibly empty) sequence of <see cref="IValidationError"/>.
  /// Returns an empty sequence when validation succeeds. Never returns <see langword="null"/>.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
  /// </exception>
  /// <exception cref="OperationCanceledException">
  /// Thrown when the operation is canceled via <paramref name="cancellationToken"/>.
  /// </exception>
  public Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default);
}
