using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Adapts a provided asynchronous delegate into an <see cref="IAsyncValidationRule{T}"/> instance.
/// </summary>
/// <typeparam name="T">Domain object type being validated.</typeparam>
/// <remarks>
/// <para>
/// Enables lightweight, on-the-fly rule creation without declaring a dedicated class. Useful for
/// dynamic pipelines, tests, or composition scenarios where a full type would add noise.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Delegate must honor the contract of <see cref="IAsyncValidationRule{T}"/>.</description></item>
///   <item><description>Returns a (possibly empty) sequence; never <see langword="null"/>.</description></item>
///   <item><description>Null arguments must trigger <see cref="ArgumentNullException"/>.</description></item>
///   <item><description>Cancellation is propagated (<see cref="OperationCanceledException"/>).</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Keep delegate side-effect free (idempotent).</description></item>
///   <item><description>Allocate minimally in the success path (prefer <c>Array.Empty&lt;IValidationError&gt;()</c>).</description></item>
///   <item><description>Perform I/O only when truly required; batch inside delegate if possible.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Instances are thread-safe iff the supplied delegate is stateless / immutable.
/// </para>
/// <para>
/// Performance: The wrapper adds only argument validation; invocation is a single delegate call.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var notEmptyRule = new AsyncCustomRule&lt;User&gt;(static (u, ctx, ct) => {
///   if (string.IsNullOrWhiteSpace(u.Email)) {
///     return Task.FromResult&lt;IEnumerable&lt;IValidationError&gt;&gt;(new IValidationError[] {
///       new ValidationError("Email", "REQUIRED", "Email is required")
///     });
///   }
///   return Task.FromResult&lt;IEnumerable&lt;IValidationError&gt;&gt;(Array.Empty&lt;IValidationError&gt;());
/// });
/// </code>
/// </example>
public sealed class AsyncCustomRule<T>: IAsyncValidationRule<T> {
  private readonly Func<T, IValidationContext, CancellationToken, Task<IEnumerable<IValidationError>>> _ValidateFunc;

  /// <summary>
  /// Creates a rule from the provided asynchronous validation delegate.
  /// </summary>
  /// <param name="validateFunc">Delegate implementing the rule logic.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="validateFunc"/> is null.</exception>
  /// <remarks>
  /// The delegate arguments correspond to (instance, context, cancellationToken) and must return a
  /// non-null task producing a non-null (possibly empty) sequence of <see cref="IValidationError"/>.
  /// </remarks>
  public AsyncCustomRule(
    Func<T, IValidationContext, CancellationToken, Task<IEnumerable<IValidationError>>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validateFunc);
    _ValidateFunc = validateFunc;
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);
    var result = await _ValidateFunc(instance, context, cancellationToken).ConfigureAwait(false);
    if (result is null) {
      var empty = Array.Empty<IValidationError>();
      return empty;
    }
    return result;
  }
}
