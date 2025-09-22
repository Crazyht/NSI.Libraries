using System.Diagnostics.CodeAnalysis;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation;

/// <summary>
/// Aggregates synchronous and asynchronous validation rules for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Validated object type.</typeparam>
/// <remarks>
/// <para>
/// Provides a fluent API to register rule instances (<see cref="IValidationRule{T}"/> and
/// <see cref="IAsyncValidationRule{T}"/>) and to execute them either synchronously (blocking async
/// rules) or asynchronously (running async rules in parallel after synchronous rule evaluation).
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Rule execution order is stable: all sync rules in registration order, then async rules.</description></item>
///   <item><description>Validation success => zero collected errors (returns <see cref="ValidationResult.Success"/>).</description></item>
///   <item><description>Business failures are encoded as <see cref="IValidationError"/>; exceptions only for programmer errors.</description></item>
///   <item><description>Async variant honors <see cref="CancellationToken"/>; sync variant ignores cancellation.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer stateless / immutable rule instances for reuse.</description></item>
///   <item><description>Add only CPU-bound rules via <see cref="AddRule"/>; use <see cref="AddAsyncRule"/> for I/O.</description></item>
///   <item><description>Return early with <see cref="ValidationResult.Success"/> to avoid extra allocations.</description></item>
///   <item><description>Keep rules side-effect free; they may be re-run or executed in parallel (async).</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: The validator is NOT thread-safe for concurrent rule registration. After rules
/// are configured (publishing phase complete) it can be used concurrently for validation executions
/// provided the contained rules are themselves thread-safe.</para>
/// <para>Performance: Minimizes allocations by pre-sizing error list capacity and reusing the shared
/// success instance. Async path batches awaiting via <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>.</para>
/// </remarks>
public class Validator<T>: IValidator<T> {
  private readonly List<IValidationRule<T>> _SyncRules = [];
  private readonly List<IAsyncValidationRule<T>> _AsyncRules = [];

  /// <summary>Adds a synchronous rule (fluent).</summary>
  /// <exception cref="ArgumentNullException">When <paramref name="rule"/> is null.</exception>
  public Validator<T> AddRule(IValidationRule<T> rule) {
    ArgumentNullException.ThrowIfNull(rule);
    _SyncRules.Add(rule);
    return this;
  }

  /// <summary>Adds an asynchronous rule (fluent).</summary>
  /// <exception cref="ArgumentNullException">When <paramref name="rule"/> is null.</exception>
  public Validator<T> AddAsyncRule(IAsyncValidationRule<T> rule) {
    ArgumentNullException.ThrowIfNull(rule);
    _AsyncRules.Add(rule);
    return this;
  }

  /// <inheritdoc />
  /// <remarks>
  /// Blocks asynchronous rules using <c>GetAwaiter().GetResult()</c>. Prefer
  /// <see cref="ValidateAsync"/> in I/O heavy scenarios to avoid thread pool starvation.
  /// </remarks>
  [SuppressMessage(
    "Blocker Code Smell",
    "S4462:Calls to \"async\" methods should not be blocking",
    Justification = "Synchronous API contract intentionally allows blocking for compatibility.")]
  public IValidationResult Validate(T instance, IValidationContext? context = null) {
    ArgumentNullException.ThrowIfNull(instance);
    context ??= ValidationContext.Empty();

    var totalRuleCount = _SyncRules.Count + _AsyncRules.Count;
    // Fast path: no rules.
    if (totalRuleCount == 0) {
      return ValidationResult.Success;
    }

    var errors = new List<IValidationError>(capacity: Math.Max(totalRuleCount, 4));

    // Execute sync rules.
    foreach (var rule in _SyncRules) {
      errors.AddRange(rule.Validate(instance, context));
    }

    // Execute async rules synchronously (blocking).
    foreach (var rule in _AsyncRules) {
      var result = rule.ValidateAsync(instance, context, CancellationToken.None).GetAwaiter().GetResult();
      errors.AddRange(result);
    }

    return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(errors);
  }

  /// <inheritdoc />
  public async Task<IValidationResult> ValidateAsync(
    T instance,
    IValidationContext? context = null,
    CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(instance);
    cancellationToken.ThrowIfCancellationRequested();
    context ??= ValidationContext.Empty();

    var totalRuleCount = _SyncRules.Count + _AsyncRules.Count;
    if (totalRuleCount == 0) {
      return ValidationResult.Success;
    }

    var errors = new List<IValidationError>(capacity: Math.Max(totalRuleCount, 4));

    // Sync rules first.
    foreach (var rule in _SyncRules) {
      cancellationToken.ThrowIfCancellationRequested();
      errors.AddRange(rule.Validate(instance, context));
    }

    if (_AsyncRules.Count > 0) {
      var tasks = new Task<IEnumerable<IValidationError>>[_AsyncRules.Count];
      for (var i = 0; i < _AsyncRules.Count; i++) {
        cancellationToken.ThrowIfCancellationRequested();
        tasks[i] = _AsyncRules[i].ValidateAsync(instance, context, cancellationToken);
      }

      var results = await Task.WhenAll(tasks).ConfigureAwait(false);
      foreach (var result in results) {
        errors.AddRange(result);
      }
    }

    return errors.Count == 0 ? ValidationResult.Success : new ValidationResult(errors);
  }
}
