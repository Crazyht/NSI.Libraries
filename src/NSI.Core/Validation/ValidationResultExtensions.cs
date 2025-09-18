using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation;

/// <summary>
/// High-level utility extensions for <see cref="IValidationResult"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Provides convenience helpers for common consumption patterns: summarizing messages, filtering
/// per property, throwing standardized exceptions, and reshaping errors for presentation layers.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>All methods are pure and never mutate the supplied result.</description></item>
///   <item><description>Empty results yield stable empty collections / strings (never null).</description></item>
///   <item><description>Ordering of underlying <see cref="IValidationError"/> objects is preserved.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Use <see cref="ThrowIfInvalid(IValidationResult)"/> only at service / boundary layers.</description></item>
///   <item><description>Prefer structured data (<see cref="ToErrorDictionary"/>) over joined message strings for APIs.</description></item>
///   <item><description>Do not localize concatenated messages—localize by individual <c>ErrorCode</c>.</description></item>
///   <item><description>For large error sets, enumerate directly instead of materializing dictionaries.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Methods are stateless &amp; reentrant; safe for concurrent use.</para>
/// <para>Performance: Fast-path checks avoid unnecessary allocations (e.g. no join when zero / one error).</para>
/// </remarks>
public static class ValidationResultExtensions {
  /// <summary>
  /// Returns a single string aggregating all error messages separated by <paramref name="separator"/>.
  /// </summary>
  /// <param name="result">Validation result (not null).</param>
  /// <param name="separator">Separator text (default: "; ").</param>
  /// <returns>
  /// <c>string.Empty</c> when valid; the sole message when exactly one error; otherwise a joined string.
  /// </returns>
  /// <exception cref="ArgumentNullException">When <paramref name="result"/> is null.</exception>
  public static string GetErrorMessage(this IValidationResult result, string separator = "; ") {
    ArgumentNullException.ThrowIfNull(result);

    var errors = result.Errors;
    var count = errors.Count;
    if (count == 0) {
      return string.Empty;
    }
    if (count == 1) {
      return errors[0].ErrorMessage;
    }
    // Allocate only once for multi-error case.
    return string.Join(separator, errors.Select(e => e.ErrorMessage));
  }

  /// <summary>
  /// Filters errors for a specific property name (exact match).
  /// </summary>
  /// <param name="result">Validation result (not null).</param>
  /// <param name="propertyName">Target property logical path (dot notation).</param>
  /// <returns>Deferred enumeration of matching errors (may be empty).</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="result"/> is null.</exception>
  /// <exception cref="ArgumentException">When <paramref name="propertyName"/> is null/whitespace.</exception>
  public static IEnumerable<IValidationError> GetErrorsForProperty(
    this IValidationResult result,
    string propertyName) {
    ArgumentNullException.ThrowIfNull(result);
    ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

    // Deferred filtering keeps allocation minimal.
    return result.Errors.Where(e => e.PropertyName == propertyName);
  }

  /// <summary>
  /// Throws <see cref="ValidationException"/> when the result is invalid.
  /// </summary>
  /// <param name="result">Validation result (not null).</param>
  /// <exception cref="ArgumentNullException">When <paramref name="result"/> is null.</exception>
  /// <exception cref="ValidationException">When <see cref="IValidationResult.IsValid"/> is false.</exception>
  public static void ThrowIfInvalid(this IValidationResult result) {
    ArgumentNullException.ThrowIfNull(result);
    if (!result.IsValid) {
      throw new ValidationException(result);
    }
  }

  /// <summary>
  /// Groups errors by property name into a dictionary (empty string key for entity-level errors).
  /// </summary>
  /// <param name="result">Validation result (not null).</param>
  /// <returns>Dictionary mapping property name to list of errors (never null).</returns>
  /// <exception cref="ArgumentNullException">When <paramref name="result"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// Uses a single pass over the error list; preserves intra-group ordering. Capacity of lists grows
  /// as needed; typical size per key is expected to be small.
  /// </para>
  /// </remarks>
  public static Dictionary<string, List<IValidationError>> ToErrorDictionary(
    this IValidationResult result) {
    ArgumentNullException.ThrowIfNull(result);

    var errors = result.Errors;
    if (errors.Count == 0) {
      return [];
    }

    var dict = new Dictionary<string, List<IValidationError>>(capacity: Math.Min(errors.Count, 16));
    foreach (var err in errors) {
      var key = err.PropertyName ?? string.Empty;
      if (!dict.TryGetValue(key, out var list)) {
        list = new List<IValidationError>(capacity: 2);
        dict[key] = list;
      }
      list.Add(err);
    }
    return dict;
  }
}
