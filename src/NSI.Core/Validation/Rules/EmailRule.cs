using System.Linq.Expressions;
using System.Text.RegularExpressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Validates that a string property of <typeparamref name="T"/> has a syntactically valid email format.
/// </summary>
/// <typeparam name="T">Parent object type containing the email property.</typeparam>
/// <remarks>
/// <para>
/// Uses a precompiled (<see cref="GeneratedRegexAttribute"/>) case-insensitive pattern to perform a
/// lightweight syntactic check (local@domain.tld). It does NOT guarantee deliverability, MX record
/// presence, mailbox existence, or RFC 5322 full compliance—intentionally simplified for common
/// application scenarios. Combine with a separate Required rule if non-null / non-empty enforcement
/// is desired.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null / empty / whitespace values are treated as valid (no error produced).</description></item>
///   <item><description>Non-empty values failing the regex produce one <see cref="IValidationError"/>.</description></item>
///   <item><description>Error code: <c>INVALID_EMAIL</c>.</description></item>
///   <item><description><see cref="IValidationError.ExpectedValue"/> contains an illustrative pattern sample.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Layer this rule after a Required rule when the field must be populated.</description></item>
///   <item><description>For stricter validation (IDN, length limits) create a specialized rule.</description></item>
///   <item><description>Prefer server-side canonicalization (e.g. trim) before invoking validation.</description></item>
///   <item><description>Avoid over-validating; defer deliverability checks to confirmation workflows.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Stateless and immutable; safe for reuse across threads.
/// </para>
/// <para>
/// Performance: Single property access + compiled regex test when non-empty. Success path (empty
/// or already valid) allocates nothing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register and use in a pipeline
/// var rule = new EmailRule&lt;User&gt;(u => u.Email);
/// foreach (var error in rule.Validate(user, context)) {
///   Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage} ({error.ErrorCode})");
/// }
/// </code>
/// </example>
public sealed partial class EmailRule<T>: IValidationRule<T> {
  private readonly Func<T, string?> _PropertyAccessor;
  private readonly string _PropertyName;

  [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
  private static partial Regex EmailRegex();

  /// <summary>
  /// Creates the rule for the specified string property expression.
  /// </summary>
  /// <param name="propertyExpression">Member access expression resolving the email property.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyExpression"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when the expression is not a simple member access.</exception>
  public EmailRule(Expression<Func<T, string?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
  }

  /// <inheritdoc/>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    // Treat null/empty/whitespace as valid (use separate Required rule if needed)
    if (string.IsNullOrWhiteSpace(value)) {
      yield break;
    }

    if (!EmailRegex().IsMatch(value)) {
      const string sample = "valid@email.com";
      yield return new ValidationError(
        "INVALID_EMAIL",
        $"{_PropertyName} must be a valid email address.",
        _PropertyName,
        sample
      );
    }
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
