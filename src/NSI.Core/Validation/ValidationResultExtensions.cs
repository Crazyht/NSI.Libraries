using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation {
  /// <summary>
  /// Provides extension methods for <see cref="IValidationResult"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class contains utility methods that extend the functionality of validation results,
  /// making it easier to work with validation errors in different contexts.
  /// </para>
  /// <para>
  /// Common uses include:
  /// <list type="bullet">
  ///   <item><description>Converting validation errors to user-friendly messages</description></item>
  ///   <item><description>Extracting errors for specific properties</description></item>
  ///   <item><description>Converting validation results to exceptions</description></item>
  ///   <item><description>Reorganizing errors for different presentation formats</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// var result = validator.Validate(user);
  /// 
  /// // Get a combined error message
  /// string message = result.GetErrorMessage();
  /// 
  /// // Get errors only for a specific property
  /// var emailErrors = result.GetErrorsForProperty("Email");
  /// 
  /// // Convert to exception if invalid
  /// result.ThrowIfInvalid();
  /// 
  /// // Group errors by property
  /// var errorsByProperty = result.ToErrorDictionary();
  /// </code>
  /// </para>
  /// </remarks>
  public static class ValidationResultExtensions {
    /// <summary>
    /// Gets all error messages as a single string.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="separator">The separator to use between messages (defaults to semicolon and space).</param>
    /// <returns>A string containing all error messages, or an empty string if there are no errors.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method concatenates all error messages from the validation result
    /// into a single string, separated by the specified separator.
    /// </para>
    /// <para>
    /// It's useful for displaying a summary of validation errors to users
    /// or including error information in logs.
    /// </para>
    /// </remarks>
    public static string GetErrorMessage(this IValidationResult result, string separator = "; ") {
      ArgumentNullException.ThrowIfNull(result);
      return string.Join(separator, result.Errors.Select(e => e.ErrorMessage));
    }

    /// <summary>
    /// Gets errors for a specific property.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="propertyName">The property name to filter by.</param>
    /// <returns>
    /// A collection of validation errors for the specified property.
    /// Returns an empty collection if no errors exist for the property.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="propertyName"/> is null or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method filters the validation errors to include only those
    /// that apply to the specified property.
    /// </para>
    /// <para>
    /// It's useful for field-level validation in user interfaces,
    /// where errors need to be displayed next to specific form fields.
    /// </para>
    /// </remarks>
    public static IEnumerable<IValidationError> GetErrorsForProperty(
      this IValidationResult result,
      string propertyName) {
      ArgumentNullException.ThrowIfNull(result);
      ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

      return result.Errors.Where(e => e.PropertyName == propertyName);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if validation failed.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is null.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when <see cref="IValidationResult.IsValid"/> is false.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method provides a convenient way to convert validation failures
    /// into exceptions that can interrupt the application flow.
    /// </para>
    /// <para>
    /// It's particularly useful in service methods that should not
    /// proceed with business logic if validation has failed.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// public async Task&lt;Order&gt; CreateOrderAsync(OrderRequest request) {
    ///   var validationResult = _validator.Validate(request);
    ///   validationResult.ThrowIfInvalid(); // Stops execution if invalid
    ///   
    ///   // Continue with creating the order...
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static void ThrowIfInvalid(this IValidationResult result) {
      ArgumentNullException.ThrowIfNull(result);

      if (!result.IsValid) {
        throw new ValidationException(result);
      }
    }

    /// <summary>
    /// Converts the validation result to a dictionary of errors grouped by property.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>
    /// A dictionary where keys are property names and values are lists of validation errors.
    /// For entity-level errors (not tied to a specific property), the key is an empty string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method reorganizes validation errors into a dictionary structure
    /// where each key is a property name and the value is a list of errors
    /// for that property.
    /// </para>
    /// <para>
    /// It's especially useful for:
    /// <list type="bullet">
    ///   <item><description>Converting validation results to a format suitable for API responses</description></item>
    ///   <item><description>Displaying errors grouped by field in user interfaces</description></item>
    ///   <item><description>Identifying which properties have the most validation issues</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static Dictionary<string, List<IValidationError>> ToErrorDictionary(
      this IValidationResult result) {
      ArgumentNullException.ThrowIfNull(result);

      return result.Errors
        .GroupBy(e => e.PropertyName ?? string.Empty)
        .ToDictionary(
          g => g.Key,
          g => g.ToList()
        );
    }
  }
}
