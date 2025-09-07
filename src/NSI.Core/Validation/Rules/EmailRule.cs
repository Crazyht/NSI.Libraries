using System.Linq.Expressions;
using System.Text.RegularExpressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;
/// <summary>
/// Validates that a string property contains a valid email address.
/// </summary>
/// <typeparam name="T">The type of object being validated.</typeparam>
/// <remarks>
/// <para>
/// This rule validates that a string property of an object contains a syntactically
/// valid email address by checking it against a regular expression pattern.
/// </para>
/// <para>
/// The validation uses a simple regex pattern that checks for:
/// <list type="bullet">
///   <item><description>Characters before the @ symbol</description></item>
///   <item><description>The @ symbol</description></item>
///   <item><description>Domain name with at least one period</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var emailRule = new EmailRule&lt;User&gt;(u => u.Email);
/// var errors = emailRule.Validate(user, validationContext);
/// </code>
/// </para>
/// </remarks>
public sealed partial class EmailRule<T>: IValidationRule<T> {
  private readonly Func<T, string?> _PropertyAccessor;
  private readonly string _PropertyName;

  [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
  private static partial Regex EmailRegex();

  /// <summary>
  /// Initializes a new instance of the <see cref="EmailRule{T}"/> class.
  /// </summary>
  /// <param name="propertyExpression">Expression to access the email property.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="propertyExpression"/> is not a valid member expression.
  /// </exception>
  /// <remarks>
  /// The property expression should be a simple member access expression
  /// (e.g., <c>x => x.Email</c>) that returns a string.
  /// </remarks>
  public EmailRule(Expression<Func<T, string?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(propertyExpression);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// This method validates that the string property identified by the property expression
  /// contains a valid email address format. It returns validation errors when:
  /// <list type="bullet">
  ///   <item><description>The property value is not null or empty, but doesn't match the email pattern</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Note that this rule doesn't validate if the email actually exists or is deliverable,
  /// only that it has a syntactically valid format.
  /// </para>
  /// </remarks>
  public IEnumerable<IValidationError> Validate(T instance, IValidationContext context) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    if (!string.IsNullOrEmpty(value) && !EmailRegex().IsMatch(value)) {
      yield return new ValidationError(
        "INVALID_EMAIL",
        $"{_PropertyName} must be a valid email address.",
        _PropertyName,
        "valid@email.com"
      );
    }
  }

  private static string GetPropertyName(Expression<Func<T, string?>> expression) =>
    expression.Body is MemberExpression member
      ? member.Member.Name
      : throw new ArgumentException("Invalid property expression");
};
