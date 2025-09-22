namespace NSI.Core.Validation.Abstractions;

/// <summary>
/// Defines a synchronous validation rule that evaluates a single business or data constraint.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
/// <remarks>
/// <para>
/// A rule focuses on one validation concern (Single Responsibility) and emits zero or more
/// <see cref="IValidationError"/> instances describing failures. Synchronous rules should perform
/// only CPU‑bound or trivially fast operations; for I/O or async work use
/// <see cref="IAsyncValidationRule{T}"/>.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Return an empty sequence (never <see langword="null"/>) when validation passes.</description></item>
///   <item><description>Yield one <see cref="IValidationError"/> per discrete failure condition.</description></item>
///   <item><description>Do not throw for normal validation failures—encode them as errors.</description></item>
///   <item><description>Throw only for programmer errors (e.g. required dependencies are null).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Keep logic side‑effect free; rules may be re‑run.</description></item>
///   <item><description>Avoid allocations in success path (branch early).</description></item>
///   <item><description>Use dot notation for nested property names (e.g. <c>Address.City</c>).</description></item>
///   <item><description>Prefer iterator (<c>yield return</c>) for conditional error emission.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Implementations should be stateless or immutable; shared instances can then be
/// reused safely across threads.
/// </para>
/// <para>
/// Performance: Minimize allocations; avoid creating collections when there are no failures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class EmailFormatRule: IValidationRule&lt;User&gt; {
///   private static readonly Regex Pattern =
///     new("^[^@\\n]+@[^@\\n]+\\.[^@\\n]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
///
///   public IEnumerable&lt;IValidationError&gt; Validate(User instance, IValidationContext context) {
///     ArgumentNullException.ThrowIfNull(instance);
///     ArgumentNullException.ThrowIfNull(context);
///
///     if (string.IsNullOrWhiteSpace(instance.Email)) {
///       yield return new ValidationError("Email", "REQUIRED", "Email is required");
///       yield break;
///     }
///     if (!Pattern.IsMatch(instance.Email)) {
///       yield return new ValidationError("Email", "INVALID_FORMAT", "Email format is invalid");
///     }
///   }
/// }
/// </code>
/// </example>
public interface IValidationRule<T> {
  /// <summary>
  /// Validates the specified <paramref name="instance"/> and returns any failures.
  /// </summary>
  /// <param name="instance">Object to validate (never null).</param>
  /// <param name="context">Ambient validation context (never null).</param>
  /// <returns>
  /// A (possibly empty) sequence of <see cref="IValidationError"/>. Empty when validation succeeds.
  /// Never <see langword="null"/>.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
  /// </exception>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context);
}
