namespace NSI.Core.Validation.Abstractions {
  /// <summary>
  /// Defines a validation rule that can be applied to an object.
  /// </summary>
  /// <typeparam name="T">The type of object to validate.</typeparam>
  /// <remarks>
  /// <para>
  /// This interface defines a synchronous validation rule that evaluates a specific
  /// business or data integrity constraint on a domain object. Each implementation
  /// should represent a single, focused validation rule.
  /// </para>
  /// <para>
  /// Validation rules can be combined and executed in sequence to perform complex
  /// validation operations. For asynchronous validation operations, use
  /// <see cref="IAsyncValidationRule{T}"/> instead.
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// public class EmailFormatRule : IValidationRule&lt;User&gt; {
  ///   public IEnumerable&lt;IValidationError&gt; Validate(User instance, IValidationContext context) {
  ///     if (instance?.Email == null || !IsValidEmail(instance.Email)) {
  ///       yield return new ValidationError {
  ///         PropertyName = nameof(User.Email),
  ///         ErrorCode = "INVALID_FORMAT",
  ///         ErrorMessage = "Email address has an invalid format."
  ///       };
  ///     }
  ///   }
  /// }
  /// </code>
  /// </para>
  /// </remarks>
  public interface IValidationRule<T> {
    /// <summary>
    /// Validates the specified object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="context">The validation context containing additional information.</param>
    /// <returns>
    /// A collection of validation errors. Returns an empty collection when validation passes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs validation on the provided instance and returns any
    /// validation errors that are found. If validation passes, an empty collection
    /// should be returned, not null.
    /// </para>
    /// <para>
    /// The validation context can be used to access dependencies or additional data
    /// needed for validation logic.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="instance"/> or <paramref name="context"/> is null.
    /// </exception>
    public IEnumerable<IValidationError> Validate(T instance, IValidationContext context);
  }
}
