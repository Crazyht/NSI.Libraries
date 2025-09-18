using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Adapts a synchronous delegate into an <see cref="IValidationRule{T}"/> for lightweight rule composition.
/// </summary>
/// <typeparam name="T">Domain object type being validated.</typeparam>
/// <remarks>
/// <para>
/// Enables on-the-fly creation of focused validation rules without a dedicated class. Useful for
/// dynamic pipelines, tests, prototyping, or composing conditional logic around existing validators.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Delegate receives (instance, context) and returns a (possibly empty) sequence.</description></item>
///   <item><description>Returned sequence must never be <see langword="null"/> (enforced here defensively).</description></item>
///   <item><description>Normal validation failures are encoded as <see cref="IValidationError"/> objects (no throwing).</description></item>
///   <item><description>Argument nulls are programmer errors and raise <see cref="ArgumentNullException"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Keep delegate side‑effect free and idempotent; it may be re‑run.</description></item>
///   <item><description>Short‑circuit early on success paths to minimize allocations.</description></item>
///   <item><description>Prefer yielding lazily when composing more complex logic; simple rules may return arrays.</description></item>
///   <item><description>Use stable UPPER_SNAKE_CASE error codes to support client handling.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Instance is thread-safe if the supplied delegate is stateless / immutable.
/// </para>
/// <para>
/// Performance: Invocation overhead is a single delegate call plus null and defensive checks.
/// </para>
/// </remarks>
public sealed class CustomRule<T>: IValidationRule<T> {
  private readonly Func<T, IValidationContext, IEnumerable<IValidationError>> _ValidateFunc;

  /// <summary>
  /// Creates a custom rule backed by the provided synchronous delegate.
  /// </summary>
  /// <param name="validateFunc">Delegate implementing the rule logic.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="validateFunc"/> is null.</exception>
  /// <remarks>
  /// The delegate contract mirrors <see cref="IValidationRule{T}.Validate"/> and must not return null.
  /// </remarks>
  public CustomRule(Func<T, IValidationContext, IEnumerable<IValidationError>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validateFunc);
    _ValidateFunc = validateFunc;
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);
    var result = _ValidateFunc(instance, context);
    return result ?? [];
  }
}
