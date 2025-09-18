using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Asynchronously validates a nested reference-type property of <typeparamref name="T"/> using a registered validator.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <typeparam name="TProperty">Nested reference type to validate.</typeparam>
/// <remarks>
/// <para>
/// Resolves an <see cref="IValidator{TProperty}"/> from <see cref="IValidationContext.ServiceProvider"/> and
/// applies both its synchronous and asynchronous rules via <see cref="IValidator{T}.ValidateAsync"/>.
/// Property names in produced <see cref="IValidationError"/> instances are prefixed with the parent
/// property path (e.g. <c>Address.City</c>).
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null nested value is treated as success (no errors emitted).</description></item>
///   <item><description>Missing validator registration yields success (fail-open by design).</description></item>
///   <item><description>Each child error <c>PropertyName</c> is prefixed with <c>{Parent}.{Child}</c>.</description></item>
///   <item><description>Returns an empty sequence (never <see langword="null"/>) on success.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Chain with a Required rule if null should be considered invalid.</description></item>
///   <item><description>Keep child validators idempotent and side-effect free.</description></item>
///   <item><description>Prefer caching expensive services in the child validator, not here.</description></item>
///   <item><description>Use stable UPPER_SNAKE_CASE error codes in underlying rules.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: The rule is immutable and thread-safe assuming the resolved validator is thread-safe.
/// </para>
/// <para>
/// Performance: Single delegate invocation + optional validator resolution. Avoids allocations on the
/// success path. Error projection allocates only when child errors exist.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new NestedAsyncValidationRule&lt;Order, Address&gt;(o => o.ShippingAddress);
/// var errors = await rule.ValidateAsync(order, context, ct);
/// </code>
/// </example>
public sealed class NestedAsyncValidationRule<T, TProperty>: IAsyncValidationRule<T>
  where TProperty : class {
  private readonly Func<T, TProperty?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Creates the rule for the specified nested property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access expression (e.g. <c>x => x.Child</c>).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when the expression is not a simple member access.</exception>
  public NestedAsyncValidationRule(Expression<Func<T, TProperty?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);
    if (value is null) {
      var empty = Array.Empty<IValidationError>();
      return empty;
    }

    var validator = context.ServiceProvider?.GetService<IValidator<TProperty>>();
    if (validator is null) {
      var empty = Array.Empty<IValidationError>();
      return empty;
    }

    var result = await validator.ValidateAsync(value, context, cancellationToken).ConfigureAwait(false);
    if (result.Errors.Count == 0) {
      var empty = Array.Empty<IValidationError>();
      return empty;
    }

    // Prefix child property names.
    var list = new List<IValidationError>(result.Errors.Count);
    foreach (var e in result.Errors) {
      list.Add(new ValidationError(
        e.ErrorCode,
        e.ErrorMessage,
        string.IsNullOrEmpty(e.PropertyName) ? _PropertyName : $"{_PropertyName}.{e.PropertyName}",
        e.ExpectedValue
      ));
    }
    return list;
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
