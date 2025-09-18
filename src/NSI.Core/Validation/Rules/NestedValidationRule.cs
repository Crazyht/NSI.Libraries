using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates a nested reference-type property of <typeparamref name="T"/> using a registered validator.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <typeparam name="TProperty">Nested reference type to validate.</typeparam>
/// <remarks>
/// <para>
/// Resolves an <see cref="IValidator{TProperty}"/> instance from the ambient
/// <see cref="IValidationContext.ServiceProvider"/> and executes it over the referenced child
/// object. Any emitted <see cref="IValidationError"/> items have their <c>PropertyName</c>
/// prefixed with the parent member (e.g. <c>Address.City</c>).
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null nested value short‑circuits (consider composing with a Required rule).</description></item>
///   <item><description>Missing validator registration is treated as success (fail‑open by design).</description></item>
///   <item><description>Errors are projected with parent prefix; child empty <c>PropertyName</c> becomes the parent name.</description></item>
///   <item><description>Returns a lazy sequence; no allocation on success path.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Keep child validators idempotent and side‑effect free.</description></item>
///   <item><description>Prefer narrow child validators; avoid large aggregate responsibilities.</description></item>
///   <item><description>Combine with explicit null checks when null indicates a failure condition.</description></item>
///   <item><description>Use stable UPPER_SNAKE_CASE error codes in child rules.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Immutable and thread-safe assuming the resolved validator is thread-safe.
/// </para>
/// <para>
/// Performance: Single delegate invocation + optional service resolution; iterator approach avoids
/// intermediate collection allocation when there are zero errors.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new NestedValidationRule&lt;Order, Address&gt;(o => o.BillingAddress);
/// foreach (var error in rule.Validate(order, context)) {
///   Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class NestedValidationRule<T, TProperty>: IValidationRule<T>
  where TProperty : class {
  private readonly Func<T, TProperty?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Creates the rule for the specified nested property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access expression (e.g. <c>x => x.Child</c>).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when the expression is not a simple member access.</exception>
  public NestedValidationRule(Expression<Func<T, TProperty?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);
    if (value is null) {
      yield break; // Nothing to validate
    }

    var validator = context.ServiceProvider?.GetService<IValidator<TProperty>>();
    if (validator is null) {
      yield break; // No validator registered => treated as success
    }

    var result = validator.Validate(value, context);
    if (result.Errors.Count == 0) {
      yield break; // Fast path: no projection needed
    }

    foreach (var e in result.Errors) {
      var prefixed = string.IsNullOrEmpty(e.PropertyName)
        ? _PropertyName
        : $"{_PropertyName}.{e.PropertyName}";

      yield return new ValidationError(
        e.ErrorCode,
        e.ErrorMessage,
        prefixed,
        e.ExpectedValue
      );
    }
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
