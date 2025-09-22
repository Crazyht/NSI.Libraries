using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Applies nested validation to each non-null element of a collection property.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <typeparam name="TItem">Element type contained in the target collection.</typeparam>
/// <remarks>
/// <para>
/// Resolves an <see cref="IValidator{TItem}"/> from the ambient DI container and executes it for
/// each element of the specified collection property. Property names in produced
/// <see cref="IValidationError"/> instances are prefixed with the collection path and index
/// (e.g. <c>Addresses[2].Street</c>).
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Skips validation when the collection property is null.</description></item>
///   <item><description>Silently returns success if no validator for <typeparamref name="TItem"/> is registered.</description></item>
///   <item><description>Skips (does not fail) null elements inside the collection.</description></item>
///   <item><description>Yields errors from child validator with prefixed property paths.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Keep child validators idempotent; ordering is preserved.</description></item>
///   <item><description>Avoid heavy service resolution inside the loop; validator is resolved once.</description></item>
///   <item><description>Prefer small collections for synchronous validation; use async rules for I/O.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Instance is safe for concurrent use provided the resolved validator is thread-safe.
/// </para>
/// <para>
/// Performance: Single pass iteration; no intermediate list allocations. Early exit on null
/// collection or missing validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate each Address element
/// var rule = new CollectionValidationRule&lt;User, Address&gt;(u => u.Addresses);
/// foreach (var err in rule.Validate(user, context)) {
///   Console.WriteLine(err.PropertyName);
/// }
/// </code>
/// </example>
public sealed class CollectionValidationRule<T, TItem>: IValidationRule<T>
  where TItem : class {
  private readonly Func<T, IEnumerable<TItem>?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Initializes the rule for the given collection property expression.
  /// </summary>
  /// <param name="propertyExpression">Member access expression selecting a collection.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when the expression is not a valid simple member access.</exception>
  public CollectionValidationRule(Expression<Func<T, IEnumerable<TItem>?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var collection = _PropertyAccessor(instance);
    if (collection is null) {
      yield break; // Nothing to validate
    }

    var validator = context.ServiceProvider?.GetService<IValidator<TItem>>();
    if (validator is null) {
      yield break; // No validator registered = treated as success
    }

    var index = 0;
    foreach (var item in collection) {
      if (item is null) { // Skip null element (do not produce error here)
        index++;
        continue;
      }

      var result = validator.Validate(item, context);
      if (result.Errors.Count == 0) {
        index++;
        continue; // Fast path
      }

      foreach (var error in result.Errors) {
        var prefixed = string.IsNullOrEmpty(error.PropertyName)
          ? $"{_PropertyName}[{index}]"
          : $"{_PropertyName}[{index}].{error.PropertyName}";

        yield return new ValidationError(
          error.ErrorCode,
            error.ErrorMessage,
            prefixed,
            error.ExpectedValue
        );
      }

      index++;
    }
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // In case of conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
