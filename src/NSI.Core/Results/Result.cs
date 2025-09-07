using System.Diagnostics.CodeAnalysis;

namespace NSI.Core.Results {

  /// <summary>
  /// Provides static factory methods for creating Result instances.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class contains factory methods and utility functions for working with the Result pattern.
  /// It provides convenient ways to create successful and failed results, as well as integration
  /// with exception-based code through the Try pattern.
  /// </para>
  /// </remarks>
  public static class Result {
    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful Result containing the value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null for reference types.</exception>
    /// <example>
    /// <code>
    /// var result = Result.Success(42);
    /// var stringResult = Result.Success("Hello, World!");
    /// </code>
    /// </example>
    public static Result<T> Success<T>(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <typeparam name="T">The type that the result would contain on success.</typeparam>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed Result containing the error.</returns>
    /// <example>
    /// <code>
    /// var result = Result.Failure&lt;int&gt;(new ResultError("PARSE_ERROR", "Invalid number format"));
    /// var stringResult = Result.Failure&lt;string&gt;("Operation not allowed");
    /// </code>
    /// </example>
    public static Result<T> Failure<T>(ResultError error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <typeparam name="T">The type that the result would contain on success.</typeparam>
    /// <param name="message">The error message.</param>
    /// <returns>A failed Result containing an error with the message.</returns>
    /// <exception cref="ArgumentException">Thrown when message is null or empty.</exception>
    /// <example>
    /// <code>
    /// var result = Result.Failure&lt;int&gt;("Invalid input provided");
    /// </code>
    /// </example>
    public static Result<T> Failure<T>(string message) {
      ArgumentException.ThrowIfNullOrWhiteSpace(message);
      return new(new ResultError(ErrorType.Generic, "GENERIC", message));
    }

    /// <summary>
    /// Executes the specified operation and wraps the result or exception in a Result.
    /// </summary>
    /// <typeparam name="T">The type of the operation result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>A successful Result if the operation succeeds, or a failed Result if an exception is thrown.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    /// <example>
    /// <code>
    /// var result = Result.Try(() => int.Parse("42"));
    /// var fileResult = Result.Try(() => File.ReadAllText("config.json"));
    /// </code>
    /// </example>
    [SuppressMessage(
      "Minor Code Smell",
      "S2221:\"Exception\" should not be caught",
      Justification = "In this method we want catch everything.")]
    public static Result<T> Try<T>(Func<T> operation) {
      ArgumentNullException.ThrowIfNull(operation);

      try {
        return Success(operation());
      } catch (Exception ex) {
        return Failure<T>(new ResultError(ErrorType.Generic, "EXCEPTION", ex.Message, ex));
      }
    }

    /// <summary>
    /// Executes the specified async operation and wraps the result or exception in a Result.
    /// </summary>
    /// <typeparam name="T">The type of the operation result.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>A task that represents the async operation, containing a successful Result if the operation succeeds, or a failed Result if an exception is thrown.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    /// <example>
    /// <code>
    /// var result = await Result.TryAsync(() => httpClient.GetStringAsync("https://api.example.com/data"));
    /// var fileResult = await Result.TryAsync(() => File.ReadAllTextAsync("config.json"));
    /// </code>
    /// </example>
    [SuppressMessage(
      "Minor Code Smell",
      "S2221:\"Exception\" should not be caught",
      Justification = "In this method we want catch everything.")]
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation) {
      ArgumentNullException.ThrowIfNull(operation);

      try {
        var result = await operation().ConfigureAwait(false);
        return Success(result);
      } catch (Exception ex) {
        return Failure<T>(new ResultError(ErrorType.Generic, "EXCEPTION", ex.Message, ex));
      }
    }

    /// <summary>
    /// Combines multiple results into a single result containing an array of values.
    /// If any result is a failure, returns the first failure encountered.
    /// </summary>
    /// <typeparam name="T">The type of the result values.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful Result containing all values, or the first failure encountered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
    /// <example>
    /// <code>
    /// var result1 = Result.Success(1);
    /// var result2 = Result.Success(2);
    /// var result3 = Result.Success(3);
    /// 
    /// var combined = Result.Combine(result1, result2, result3);
    /// // Returns Result.Success([1, 2, 3])
    /// </code>
    /// </example>
    public static Result<T[]> Combine<T>(params Result<T>[] results) {
      ArgumentNullException.ThrowIfNull(results);

      var values = new T[results.Length];

      for (var i = 0; i < results.Length; i++) {
        if (results[i].IsFailure) {
          return Failure<T[]>(results[i].Error);
        }
        values[i] = results[i].Value;
      }

      return Success(values);
    }

    /// <summary>
    /// Combines multiple results into a single result containing a list of values.
    /// If any result is a failure, returns the first failure encountered.
    /// </summary>
    /// <typeparam name="T">The type of the result values.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful Result containing all values, or the first failure encountered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
    /// <example>
    /// <code>
    /// var results = new List&lt;Result&lt;int&gt;&gt; {
    ///   Result.Success(1),
    ///   Result.Success(2),
    ///   Result.Success(3)
    /// };
    /// 
    /// var combined = Result.Combine(results);
    /// // Returns Result.Success([1, 2, 3])
    /// </code>
    /// </example>
    public static Result<List<T>> Combine<T>(IEnumerable<Result<T>> results) {
      ArgumentNullException.ThrowIfNull(results);

      var values = new List<T>();

      foreach (var result in results) {
        if (result.IsFailure) {
          return Failure<List<T>>(result.Error);
        }
        values.Add(result.Value);
      }

      return Success(values);
    }
  }
}
