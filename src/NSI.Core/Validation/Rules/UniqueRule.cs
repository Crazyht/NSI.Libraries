using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation.Rules;

/// <summary>
/// Asynchronously validates that a property value is unique within an external data source.
/// </summary>
/// <typeparam name="T">Parent object type.</typeparam>
/// <typeparam name="TValue">Property value type.</typeparam>
/// <remarks>
/// <para>
/// Delegates uniqueness evaluation to a supplied asynchronous predicate (<c>existsCheck</c>) that
/// typically queries a repository / data store through the provided <see cref="IServiceProvider"/>.
/// A single validation error (code <c>NOT_UNIQUE</c>) is emitted when the value already exists.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><description>Null value => success (no uniqueness check performed).</description></item>
///   <item><description>existsCheck returns true => emit <c>NOT_UNIQUE</c> error.</description></item>
///   <item><description>existsCheck returns false => success (empty result).</description></item>
///   <item><description>Never throws for a non-unique value; returns an error instead.</description></item>
/// </list>
/// </para>
/// <para>
/// Guidelines:
/// <list type="bullet">
///   <item><description>Make <c>existsCheck</c> side-effect free and idempotent.</description></item>
///   <item><description>Ensure appropriate read index / key on backing store for performance.</description></item>
///   <item><description>Handle normalization (e.g. case folding) inside <c>existsCheck</c> if required.</description></item>
///   <item><description>Compose with <see cref="RequiredRule{T}"/> if null should be disallowed.</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: Immutable and thread-safe assuming external services are thread-safe.
/// </para>
/// <para>
/// Performance: Single delegate invocation on non-null path; fast null shortcut. Avoids allocations
/// on success path by returning <see cref="Array.Empty{T}()"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rule = new UniqueRule&lt;User, string&gt;(u => u.Email,
///   async (sp, email, ct) => await sp.GetRequiredService&lt;IUserRepository&gt;()
///     .EmailExistsAsync(email, ct));
/// var errors = await rule.ValidateAsync(user, context, ct);
/// </code>
/// </example>
public sealed class UniqueRule<T, TValue>: IAsyncValidationRule<T> {
  private readonly Func<T, TValue> _PropertyAccessor;
  private readonly string _PropertyName;
  private readonly Func<IServiceProvider, TValue, CancellationToken, Task<bool>> _ExistsCheck;

  /// <summary>
  /// Creates the uniqueness validation rule for a property.
  /// </summary>
  /// <param name="propertyExpression">Simple member access (e.g. <c>x => x.Email</c>).</param>
  /// <param name="existsCheck">Async predicate returning true when the value already exists.</param>
  /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="propertyExpression"/> is not a simple member access.</exception>
  public UniqueRule(
    Expression<Func<T, TValue>> propertyExpression,
    Func<IServiceProvider, TValue, CancellationToken, Task<bool>> existsCheck) {
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentNullException.ThrowIfNull(existsCheck);

    _PropertyAccessor = propertyExpression.Compile();
    _PropertyName = GetPropertyName(propertyExpression.Body);
    _ExistsCheck = existsCheck;
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<IValidationError>> ValidateAsync(
    T instance,
    IValidationContext context,
    CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(instance);
    ArgumentNullException.ThrowIfNull(context);

    var value = _PropertyAccessor(instance);

    // Null => treat as valid (compose with RequiredRule when needed).
    if (value is null) {
      var empty = Array.Empty<IValidationError>();
      return empty;
    }

    cancellationToken.ThrowIfCancellationRequested();

    var exists = await _ExistsCheck(context.ServiceProvider, value, cancellationToken)
      .ConfigureAwait(false);

    if (!exists) {
      var empty = Array.Empty<IValidationError>();
      return empty;
    }

    var single = new IValidationError[] {
      new ValidationError(
        "NOT_UNIQUE",
        $"{_PropertyName} already exists.",
        _PropertyName,
        value
      )
    };
    return single;
  }

  private static string GetPropertyName(Expression expression) => expression switch {
    MemberExpression m => m.Member.Name,
    UnaryExpression { Operand: MemberExpression m } => m.Member.Name, // Handles conversions
    _ => throw new ArgumentException("Property expression must be a simple member access", nameof(expression))
  };
}
