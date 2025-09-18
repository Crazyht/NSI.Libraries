using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using NSI.Core.Validation.Abstractions;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Validation;

/// <summary>
/// Fluent extension methods that add common validation rules to <see cref="Validator{T}"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Provides a terse, discoverable API surface for composing validation pipelines without manually
/// instantiating rule classes. Each method wraps a concrete rule (sync or async) and returns the
/// original validator to enable chaining.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Each call appends (does not replace) a rule to the validator.</description></item>
///   <item><description>Underlying rule execution order: all sync rules, then async rules.</description></item>
///   <item><description>All methods throw <see cref="ArgumentNullException"/> on null arguments.</description></item>
///   <item><description>No validation is performed at rule registration time.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Prefer these helpers over direct rule construction for readability.</description></item>
///   <item><description>Use business‑meaningful error codes (UPPER_SNAKE_CASE).</description></item>
///   <item><description>Keep custom predicates side‑effect free and fast (I/O => async variants).</description></item>
///   <item><description>Use <see cref="ValidateNested"/> / <see cref="ValidateEach"/> sparingly to avoid deep chains.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Extensions themselves are stateless. The target validator must not be
/// mutated (adding rules) concurrently from multiple threads.</para>
/// <para>Performance: Methods allocate only the underlying rule; no eager reflection or compilation.
/// Prefer reusing configured validator instances across executions.</para>
/// </remarks>
public static class ValidatorBuilderExtensions {
  /// <summary>Adds a required-value rule for the selected property.</summary>
  public static Validator<T> Required<T>(
    this Validator<T> validator,
    Expression<Func<T, object?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    return validator.AddRule(new RequiredRule<T>(propertyExpression));
  }

  /// <summary>Adds an email format rule for a string property.</summary>
  public static Validator<T> Email<T>(
    this Validator<T> validator,
    Expression<Func<T, string?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    return validator.AddRule(new EmailRule<T>(propertyExpression));
  }

  /// <summary>Adds a bounded length rule for a string property (inclusive min/max).</summary>
  public static Validator<T> StringLength<T>(
    this Validator<T> validator,
    Expression<Func<T, string?>> propertyExpression,
    int minLength = 0,
    int maxLength = int.MaxValue) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentOutOfRangeException.ThrowIfNegative(minLength);
    ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);
    return validator.AddRule(new StringLengthRule<T>(propertyExpression, minLength, maxLength));
  }

  /// <summary>Adds an inclusive range rule for a comparable property.</summary>
  public static Validator<T> Range<T, TValue>(
    this Validator<T> validator,
    Expression<Func<T, TValue>> propertyExpression,
    TValue min,
    TValue max) where TValue : IComparable<TValue> {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    if (min.CompareTo(max) > 0) {
      throw new ArgumentOutOfRangeException(nameof(max),
        "Maximum value must be greater than or equal to minimum value");
    }
    return validator.AddRule(new RangeRule<T, TValue>(propertyExpression, min, max));
  }

  /// <summary>Adds an async uniqueness rule (value must not already exist).</summary>
  public static Validator<T> Unique<T, TValue>(
    this Validator<T> validator,
    Expression<Func<T, TValue>> propertyExpression,
    Func<IServiceProvider, TValue, CancellationToken, Task<bool>> existsCheck) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentNullException.ThrowIfNull(existsCheck);
    return validator.AddAsyncRule(new UniqueRule<T, TValue>(propertyExpression, existsCheck));
  }

  /// <summary>Adds a custom synchronous rule implemented by a delegate.</summary>
  public static Validator<T> Custom<T>(
    this Validator<T> validator,
    Func<T, IValidationContext, IEnumerable<IValidationError>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(validateFunc);
    return validator.AddRule(new CustomRule<T>(validateFunc));
  }

  /// <summary>Adds a simple predicate-based rule (fail => single error).</summary>
  public static Validator<T> Must<T>(
    this Validator<T> validator,
    Func<T, bool> predicate,
    string errorCode,
    string errorMessage,
    string? propertyName = null) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
    return validator.AddRule(new CustomRule<T>((instance, _) =>
      !predicate(instance)
        ? new IValidationError[] { new ValidationError(errorCode, errorMessage, propertyName) }
        : []));
  }

  /// <summary>Adds an async custom rule implemented by a delegate.</summary>
  [SuppressMessage("Minor Code Smell", "S4261", Justification = "Async behavior inside rule type.")]
  [SuppressMessage("Minor Code Smell", "S4017", Justification = "Generic nesting required for delegate signature.")]
  public static Validator<T> CustomAsync<T>(
    this Validator<T> validator,
    Func<T, IValidationContext, CancellationToken, Task<IEnumerable<IValidationError>>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(validateFunc);
    return validator.AddAsyncRule(new AsyncCustomRule<T>(validateFunc));
  }

  /// <summary>Adds an async predicate rule (fail => single error).</summary>
  [SuppressMessage("Minor Code Smell", "S4261", Justification = "Async behavior inside rule type.")]
  public static Validator<T> MustAsync<T>(
    this Validator<T> validator,
    Func<T, CancellationToken, Task<bool>> predicate,
    string errorCode,
    string errorMessage,
    string? propertyName = null) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
    return validator.AddAsyncRule(new AsyncCustomRule<T>(async (instance, _, ct) =>
      !await predicate(instance, ct).ConfigureAwait(false)
        ? new IValidationError[] { new ValidationError(errorCode, errorMessage, propertyName) }
        : []));
  }

  /// <summary>Validates a nested reference object (sync + async).</summary>
  public static Validator<T> ValidateNested<T, TProperty>(
    this Validator<T> validator,
    Expression<Func<T, TProperty?>> propertyExpression)
    where TProperty : class {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    return validator
      .AddRule(new NestedValidationRule<T, TProperty>(propertyExpression))
      .AddAsyncRule(new NestedAsyncValidationRule<T, TProperty>(propertyExpression));
  }

  /// <summary>Validates each element of a collection (sync rule).</summary>
  public static Validator<T> ValidateEach<T, TItem>(
    this Validator<T> validator,
    Expression<Func<T, IEnumerable<TItem>?>> propertyExpression)
    where TItem : class {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    return validator.AddRule(new CollectionValidationRule<T, TItem>(propertyExpression));
  }
}
