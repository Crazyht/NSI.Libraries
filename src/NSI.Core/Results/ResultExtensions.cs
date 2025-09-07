using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Results {
  /// <summary>
  /// Provides extension methods for working with Result types.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class contains extension methods that provide additional functionality
  /// for the Result pattern, including LINQ-style operations and specialized
  /// error handling methods.
  /// </para>
  /// </remarks>
  public static class ResultExtensions {
    /// <summary>
    /// Filters a successful result based on a predicate, converting to failure if the predicate is not satisfied.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to filter.</param>
    /// <param name="predicate">The predicate to test the value against.</param>
    /// <param name="errorFactory">Factory function to create the error if predicate fails.</param>
    /// <returns>The original result if successful and predicate passes, otherwise a failure result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate or errorFactory is null.</exception>
    /// <example>
    /// <code>
    /// var result = Result.Success(5)
    ///   .Where(x => x > 0, () => ResultError.BusinessRule("NEGATIVE_VALUE", "Value must be positive"));
    /// </code>
    /// </example>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate, Func<ResultError> errorFactory) {
      ArgumentNullException.ThrowIfNull(predicate);
      ArgumentNullException.ThrowIfNull(errorFactory);

      if (result.IsFailure) {
        return result;
      }

      return predicate(result.Value) ? result : Result.Failure<T>(errorFactory());
    }

    /// <summary>
    /// Checks if the result is a failure of a specific error type.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="errorType">The error type to check for.</param>
    /// <returns><c>true</c> if the result is a failure of the specified type; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (result.IsFailureOfType(ErrorType.NotFound)) {
    ///   // Handle not found error
    /// }
    /// </code>
    /// </example>
    public static bool IsFailureOfType<T>(this Result<T> result, ErrorType errorType) =>
      result.IsFailure && result.Error.IsOfType(errorType);

    /// <summary>
    /// Executes an action if the result is a failure of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="errorType">The error type to check for.</param>
    /// <param name="action">The action to execute if the error type matches.</param>
    /// <returns>The original result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <example>
    /// <code>
    /// var result = someOperation()
    ///   .TapErrorOfType(ErrorType.NotFound, error => _logger.LogWarning("Resource not found: {Error}", error))
    ///   .TapErrorOfType(ErrorType.Database, error => _logger.LogError("Database error: {Error}", error));
    /// </code>
    /// </example>
    public static Result<T> TapErrorOfType<T>(this Result<T> result, ErrorType errorType, Action<ResultError> action) {
      ArgumentNullException.ThrowIfNull(action);

      if (result.IsFailureOfType(errorType)) {
        action(result.Error);
      }

      return result;
    }

    /// <summary>
    /// Gets the validation errors from a result if it's a validation failure.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to extract validation errors from.</param>
    /// <returns>The validation errors if this is a validation failure; otherwise, an empty list.</returns>
    /// <example>
    /// <code>
    /// var validationErrors = result.GetValidationErrors();
    /// foreach (var error in validationErrors) {
    ///   Console.WriteLine($"{error.FieldName}: {error.Message}");
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyList<IValidationError> GetValidationErrors<T>(this Result<T> result)
      => result.IsFailure && result.Error.HasValidationErrors
        ? result.Error.ValidationErrors!
        : new List<IValidationError>().AsReadOnly();

    /// <summary>
    /// Converts a nullable value to a Result, treating null as a failure.
    /// </summary>
    /// <typeparam name="T">The type of the nullable value.</typeparam>
    /// <param name="value">The nullable value to convert.</param>
    /// <param name="errorFactory">Factory function to create the error if value is null.</param>
    /// <returns>A successful result if value is not null; otherwise, a failure result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when errorFactory is null.</exception>
    /// <example>
    /// <code>
    /// string? nullableString = GetStringOrNull();
    /// var result = nullableString.ToResult(() => ResultError.NotFound("STRING_NULL", "String value is null"));
    /// </code>
    /// </example>
    public static Result<T> ToResult<T>(this T? value, Func<ResultError> errorFactory) where T : class {
      ArgumentNullException.ThrowIfNull(errorFactory);

      return value is not null ? Result.Success(value) : Result.Failure<T>(errorFactory());
    }

    /// <summary>
    /// Converts a nullable value type to a Result, treating null as a failure.
    /// </summary>
    /// <typeparam name="T">The type of the nullable value.</typeparam>
    /// <param name="value">The nullable value to convert.</param>
    /// <param name="errorFactory">Factory function to create the error if value is null.</param>
    /// <returns>A successful result if value has a value; otherwise, a failure result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when errorFactory is null.</exception>
    /// <example>
    /// <code>
    /// int? nullableInt = GetIntOrNull();
    /// var result = nullableInt.ToResult(() => ResultError.NotFound("INT_NULL", "Integer value is null"));
    /// </code>
    /// </example>
    public static Result<T> ToResult<T>(this T? value, Func<ResultError> errorFactory) where T : struct {
      ArgumentNullException.ThrowIfNull(errorFactory);

      return value.HasValue ? Result.Success(value.Value) : Result.Failure<T>(errorFactory());
    }
  }
}
