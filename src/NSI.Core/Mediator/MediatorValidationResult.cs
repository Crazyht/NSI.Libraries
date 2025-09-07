
namespace NSI.Core.Mediator;
/// <summary>
/// Result of mediator handler validation.
/// </summary>
public class MediatorValidationResult {
  /// <summary>
  /// Gets a value indicating whether the validation was successful.
  /// </summary>
  public bool IsValid { get; set; }

  /// <summary>
  /// Gets the list of validation errors.
  /// </summary>
  public IList<string> Errors { get; } = [];

  /// <summary>
  /// Gets the number of handlers validated.
  /// </summary>
  public int ValidatedHandlerCount => Errors.Count == 0 ? 1 : 0; // Will be properly calculated in full implementation
}
