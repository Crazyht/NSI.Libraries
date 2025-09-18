using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that a required property on <typeparamref name="T"/> is non-null and, for strings, non-empty.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <remarks>
/// <para>
/// Enforces presence semantics for both reference and string properties. For string members it rejects
/// values that are <see cref="string.Empty"/> or whitespace-only. For all other reference types it only
/// enforces non-null (no deep / collection emptiness inspection).
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null reference value => one error (code <c>REQUIRED</c>).</description></item>
///   <item><description>Empty / whitespace string => one error (code <c>REQUIRED</c>).</description></item>
///   <item><description>Any other value => success (no errors).</description></item>
///   <item><description>Does not trim or mutate the underlying value.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Compose with specific rules (Length, Pattern) after presence is established.</description></item>
///   <item><description>Avoid using for value types; they cannot be null (use domain constraints instead).</description></item>
///   <item><description>For collections, create a distinct rule if minimum element count must be enforced.</description></item>
///   <item><description>Keep error code stable (<c>REQUIRED</c>) for client-side localization / mapping.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Immutable and stateless; safe for concurrent use.
/// </para>
/// <para>
/// Performance: Single delegate invocation + type test. Zero allocations in success path.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new RequiredRule&lt;User&gt;(u => u.Email);
/// foreach (var err in rule.Validate(user, context)) {
///   Console.WriteLine($"{err.PropertyName}: {err.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class RequiredRule<T>: IValidationRule<T> {
  private readonly Func<T, object?> _PropertyAccessor;
  private readonly string _PropertyName;

  /// <summary>
  /// Creates the rule for the specified property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access (e.g. <c>x => x.Name</c>).</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when the expression is not a simple member access.</exception>
  public RequiredRule(Expression<Func<T, object?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    // Fast path success checks
    if (value is not null) {
      if (value is string s && string.IsNullOrWhiteSpace(s)) {
        const string expected = "non-empty value";
        yield return new ValidationError(
          "REQUIRED",
          $"{_PropertyName} is required.",
          _PropertyName,
          expected
        );
      }
      yield break;
    }

    // Null value
    const string expectedNull = "non-empty value";
    yield return new ValidationError(
      "REQUIRED",
      $"{_PropertyName} is required.",
      _PropertyName,
      expectedNull
    );
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions / boxing
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
