namespace NSI.Core.Results;
/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
/// <para>
/// The Result pattern provides a functional approach to error handling by explicitly
/// representing success and failure states in the type system. This eliminates the need
/// for exceptions in expected error scenarios and forces developers to handle both cases.
/// </para>
/// <para>
/// Key benefits:
/// <list type="bullet">
///   <item><description>Type safety: Compile-time guarantee that errors are handled</description></item>
///   <item><description>Performance: No exception overhead for expected failures</description></item>
///   <item><description>Composability: Chain operations with automatic error propagation</description></item>
///   <item><description>Explicit: Makes error handling visible in the API</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create success result
/// var success = Result.Success(42);
/// 
/// // Create failure result
/// var failure = Result.Failure&lt;int&gt;("Invalid input");
/// 
/// // Chain operations
/// var result = ParseInteger("10")
///   .Map(x => x * 2)
///   .Bind(x => x > 15 ? Result.Success(x) : Result.Failure&lt;int&gt;("Too small"))
///   .Match(
///     success: value => $"Result: {value}",
///     failure: error => $"Error: {error.Message}"
///   );
/// </code>
/// </example>
public readonly struct Result<T>: IEquatable<Result<T>> {
  private readonly T _Value;
  private readonly ResultError _Error;

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> struct with a success value.
  /// </summary>
  /// <param name="value">The success value.</param>
  /// <exception cref="ArgumentNullException">Thrown when value is null for reference types.</exception>
  internal Result(T value) {
    if (value is null) {
      throw new ArgumentNullException(nameof(value), "Success value cannot be null");
    }

    _Value = value;
    _Error = default;
    IsSuccess = true;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> struct with an error.
  /// </summary>
  /// <param name="error">The error describing the failure.</param>
  internal Result(ResultError error) {
    _Value = default!;
    _Error = error;
    IsSuccess = false;
  }

  /// <summary>
  /// Gets a value indicating whether the result represents a success.
  /// </summary>
  /// <value><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</value>
  public bool IsSuccess { get; }

  /// <summary>
  /// Gets a value indicating whether the result represents a failure.
  /// </summary>
  /// <value><c>true</c> if the operation failed; otherwise, <c>false</c>.</value>
  public bool IsFailure => !IsSuccess;

  /// <summary>
  /// Gets the success value.
  /// </summary>
  /// <value>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the default value.</value>
  /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failure result.</exception>
  public T Value => IsSuccess ? _Value : throw new InvalidOperationException("Cannot access Value of a failure result");

  /// <summary>
  /// Gets the error information.
  /// </summary>
  /// <value>The error if <see cref="IsFailure"/> is <c>true</c>; otherwise, the default error.</value>
  /// <exception cref="InvalidOperationException">Thrown when accessing Error on a success result.</exception>
  public ResultError Error => IsFailure ? _Error : throw new InvalidOperationException("Cannot access Error of a success result");

  /// <summary>
  /// Transforms the success value using the specified function.
  /// </summary>
  /// <typeparam name="TResult">The type of the transformed value.</typeparam>
  /// <param name="mapper">The function to transform the success value.</param>
  /// <returns>A new Result with the transformed value, or the original error if this is a failure.</returns>
  /// <exception cref="ArgumentNullException">Thrown when mapper is null.</exception>
  /// <example>
  /// <code>
  /// var result = Result.Success(5)
  ///   .Map(x => x * 2); // Result.Success(10)
  /// 
  /// var failure = Result.Failure&lt;int&gt;("Error")
  ///   .Map(x => x * 2); // Still Result.Failure&lt;int&gt;("Error")
  /// </code>
  /// </example>
  public Result<TResult> Map<TResult>(Func<T, TResult> mapper) {
    ArgumentNullException.ThrowIfNull(mapper);

    return IsSuccess ? Result.Success(mapper(_Value)) : Result.Failure<TResult>(_Error);
  }

  /// <summary>
  /// Chains another Result-returning operation if this result is successful.
  /// </summary>
  /// <typeparam name="TResult">The type of the next operation's success value.</typeparam>
  /// <param name="binder">The function that returns another Result.</param>
  /// <returns>The result of the binder function if this is successful, or the original error.</returns>
  /// <exception cref="ArgumentNullException">Thrown when binder is null.</exception>
  /// <example>
  /// <code>
  /// var result = Result.Success(5)
  ///   .Bind(x => x > 0 ? Result.Success(x * 2) : Result.Failure&lt;int&gt;("Must be positive"));
  /// </code>
  /// </example>
  public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder) {
    ArgumentNullException.ThrowIfNull(binder);

    return IsSuccess ? binder(_Value) : Result.Failure<TResult>(_Error);
  }

  /// <summary>
  /// Executes one of two functions based on the result state and returns the result.
  /// </summary>
  /// <typeparam name="TResult">The type of the return value.</typeparam>
  /// <param name="onSuccess">The function to execute if the result is successful.</param>
  /// <param name="onFailure">The function to execute if the result is a failure.</param>
  /// <returns>The result of the executed function.</returns>
  /// <exception cref="ArgumentNullException">Thrown when onSuccess or onFailure is null.</exception>
  /// <example>
  /// <code>
  /// var message = result.Match(
  ///   onSuccess: value => $"Success: {value}",
  ///   onFailure: error => $"Error: {error.Message}"
  /// );
  /// </code>
  /// </example>
  public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure) {
    ArgumentNullException.ThrowIfNull(onSuccess);
    ArgumentNullException.ThrowIfNull(onFailure);

    return IsSuccess ? onSuccess(_Value) : onFailure(_Error);
  }

  /// <summary>
  /// Executes a side effect if the result is successful, without changing the result.
  /// </summary>
  /// <param name="action">The action to execute with the success value.</param>
  /// <returns>The same result instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  /// <example>
  /// <code>
  /// var result = Result.Success(42)
  ///   .Tap(value => Console.WriteLine($"Success: {value}"))
  ///   .Map(x => x * 2);
  /// </code>
  /// </example>
  public Result<T> Tap(Action<T> action) {
    ArgumentNullException.ThrowIfNull(action);

    if (IsSuccess) {
      action(_Value);
    }

    return this;
  }

  /// <summary>
  /// Executes a side effect if the result is a failure, without changing the result.
  /// </summary>
  /// <param name="action">The action to execute with the error.</param>
  /// <returns>The same result instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  /// <example>
  /// <code>
  /// var result = Result.Failure&lt;int&gt;("Error")
  ///   .TapError(error => _logger.LogError("Operation failed: {Error}", error))
  ///   .Map(x => x * 2);
  /// </code>
  /// </example>
  public Result<T> TapError(Action<ResultError> action) {
    ArgumentNullException.ThrowIfNull(action);

    if (IsFailure) {
      action(_Error);
    }

    return this;
  }

  /// <summary>
  /// Determines whether the specified <see cref="Result{T}"/> is equal to this instance.
  /// </summary>
  /// <param name="other">The other result to compare.</param>
  /// <returns><c>true</c> if the results are equal; otherwise, <c>false</c>.</returns>
  public bool Equals(Result<T> other) {
    if (IsSuccess != other.IsSuccess) {
      return false;
    }

    return IsSuccess
      ? EqualityComparer<T>.Default.Equals(_Value, other._Value)
      : _Error.Equals(other._Error);
  }

  /// <summary>
  /// Determines whether the specified object is equal to this instance.
  /// </summary>
  /// <param name="obj">The object to compare.</param>
  /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
  public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

  /// <summary>
  /// Returns a hash code for this instance.
  /// </summary>
  /// <returns>A hash code for this instance.</returns>
  public override int GetHashCode() => IsSuccess
      ? HashCode.Combine(IsSuccess, _Value)
      : HashCode.Combine(IsSuccess, _Error);

  /// <summary>
  /// Returns a string representation of this result.
  /// </summary>
  /// <returns>A string that represents this result.</returns>
  public override string ToString() => IsSuccess
      ? $"Success({_Value})"
      : $"Failure({_Error})";

  /// <summary>
  /// Determines whether two results are equal.
  /// </summary>
  /// <param name="left">The first result.</param>
  /// <param name="right">The second result.</param>
  /// <returns><c>true</c> if the results are equal; otherwise, <c>false</c>.</returns>
  public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

  /// <summary>
  /// Determines whether two results are not equal.
  /// </summary>
  /// <param name="left">The first result.</param>
  /// <param name="right">The second result.</param>
  /// <returns><c>true</c> if the results are not equal; otherwise, <c>false</c>.</returns>
  public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

  /// <summary>
  /// Implicitly converts a value to a successful result.
  /// </summary>
  /// <param name="value">The success value.</param>
  public static implicit operator Result<T>(T value) => Result.Success(value);

  /// <summary>
  /// Implicitly converts an error to a failed result.
  /// </summary>
  /// <param name="error">The error.</param>
  public static implicit operator Result<T>(ResultError error) => Result.Failure<T>(error);
}
