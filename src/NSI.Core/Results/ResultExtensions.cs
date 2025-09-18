using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Results;

/// <summary>
/// Provides functional-style extension helpers for working with <see cref="Result{T}"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Adds LINQ-like filtering, conditional tapping, validation extraction and nullable bridging
/// utilities on top of the core Result pattern without introducing additional allocations in
/// the success path.
/// </para>
/// <para>
/// Performance: All helpers short‑circuit on failure and avoid delegate invocation unless needed.
/// The validation extraction reuses a cached empty list to avoid repeated allocations.
/// </para>
/// <para>Thread-safety: All methods are pure (except user supplied actions) and therefore thread-safe.</para>
/// </remarks>
public static class ResultExtensions {
  private static readonly IReadOnlyList<IValidationError> EmptyValidationErrors =
    new List<IValidationError>(0).AsReadOnly();

  /// <summary>
  /// Filters a successful result; converts to failure if <paramref name="predicate"/> returns false.
  /// </summary>
  /// <typeparam name="T">Underlying success value type.</typeparam>
  /// <param name="result">Source result.</param>
  /// <param name="predicate">Predicate executed when <paramref name="result"/> is successful.</param>
  /// <param name="errorFactory">Factory producing the failure error when predicate fails.</param>
  /// <returns>
  /// Original success when predicate passes; failure produced by <paramref name="errorFactory"/>
  /// when predicate fails; original failure unchanged.
  /// </returns>
  /// <exception cref="ArgumentNullException">Predicate or <paramref name="errorFactory"/> is null.</exception>
  /// <example>
  /// <code>
  /// var positive = Result.Success(5)
  ///   .Where(x => x > 0, () => ResultError.BusinessRule("NEGATIVE", "Value must be positive"));
  /// </code>
  /// </example>
  public static Result<T> Where<T>(this Result<T> result,
    Func<T, bool> predicate,
    Func<ResultError> errorFactory) {
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentNullException.ThrowIfNull(errorFactory);

    if (result.IsFailure) {
      return result;
    }

    return predicate(result.Value) ? result : Result.Failure<T>(errorFactory());
  }

  /// <summary>
  /// Returns true when the result is a failure whose <see cref="ResultError.Type"/> equals
  /// <paramref name="errorType"/>.
  /// </summary>
  /// <typeparam name="T">Underlying value type.</typeparam>
  /// <param name="result">Source result.</param>
  /// <param name="errorType">Error type to test.</param>
  public static bool IsFailureOfType<T>(this Result<T> result, ErrorType errorType)
    => result.IsFailure && result.Error.IsOfType(errorType);

  /// <summary>
  /// Executes <paramref name="action"/> when the result is a failure of <paramref name="errorType"/>.
  /// </summary>
  /// <typeparam name="T">Underlying value type.</typeparam>
  /// <param name="result">Source result.</param>
  /// <param name="errorType">Target error type.</param>
  /// <param name="action">Side-effect action receiving the matching error.</param>
  /// <returns>Original <paramref name="result"/> (for chaining).</returns>
  /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
  public static Result<T> TapErrorOfType<T>(this Result<T> result,
    ErrorType errorType,
    Action<ResultError> action) {
    ArgumentNullException.ThrowIfNull(action);
    if (result.IsFailureOfType(errorType)) {
      action(result.Error);
    }
    return result;
  }

  /// <summary>
  /// Returns validation errors when the result is a validation failure; otherwise an empty list.
  /// </summary>
  /// <typeparam name="T">Underlying value type.</typeparam>
  /// <param name="result">Source result.</param>
  public static IReadOnlyList<IValidationError> GetValidationErrors<T>(this Result<T> result)
    => result.IsFailure && result.Error.HasValidationErrors
      ? result.Error.ValidationErrors!
      : EmptyValidationErrors;

  /// <summary>
  /// Converts a reference type value to a result; null becomes failure produced by
  /// <paramref name="errorFactory"/>.
  /// </summary>
  /// <typeparam name="T">Reference type.</typeparam>
  /// <param name="value">Value that may be null.</param>
  /// <param name="errorFactory">Factory creating failure error when <paramref name="value"/> is null.</param>
  /// <returns>Success when not null; failure otherwise.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="errorFactory"/> is null.</exception>
  public static Result<T> ToResult<T>(this T? value, Func<ResultError> errorFactory) where T : class {
    ArgumentNullException.ThrowIfNull(errorFactory);
    return value is not null ? Result.Success(value) : Result.Failure<T>(errorFactory());
  }

  /// <summary>
  /// Converts a nullable value type to a result; null becomes failure produced by
  /// <paramref name="errorFactory"/>.
  /// </summary>
  /// <typeparam name="T">Value type.</typeparam>
  /// <param name="value">Nullable value.</param>
  /// <param name="errorFactory">Factory creating failure error when <paramref name="value"/> is null.</param>
  /// <returns>Success when <paramref name="value"/> has a value; failure otherwise.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="errorFactory"/> is null.</exception>
  public static Result<T> ToResult<T>(this T? value, Func<ResultError> errorFactory) where T : struct {
    ArgumentNullException.ThrowIfNull(errorFactory);
    return value.HasValue ? Result.Success(value.Value) : Result.Failure<T>(errorFactory());
  }
}
