using System.Diagnostics.CodeAnalysis;

namespace NSI.Domains.StrongIdentifier {
  /// <summary>
  /// Marker interface for implementing the strongly-typed ID pattern.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This interface enables type-safe identifiers throughout the application by providing
  /// a common base type for ID classes. It helps prevent primitive obsession and ensures 
  /// that different entity IDs cannot be accidentally mixed or confused.
  /// </para>
  /// <para>
  /// Used in conjunction with <see cref="StronglyTypedId{TId, TUnderlying}"/>, this interface
  /// facilitates:
  /// </para>
  /// <list type="bullet">
  ///   <item><description>Type-safety for ID parameters and properties</description></item>
  ///   <item><description>Generic constraints for base classes like <see cref="Entity{TId}"/></description></item>
  ///   <item><description>Automatic conversions for Entity Framework Core via <c>NSI.EntityFramework.Converters.StronglyTypedIdValueConverter&lt;TId, TUnderlying&gt;</c></description></item>
  ///   <item><description>Seamless JSON serialization through <c>NSI.Json.StronglyTypedIdJsonConverterFactory</c></description></item>
  /// </list>
  /// </remarks>
  /// <example>
  /// Usage example:
  /// <code>
  /// // Define a strongly-typed ID for users
  /// public sealed record UserId(Guid Value) : StronglyTypedId&lt;UserId, Guid>(Value);
  /// 
  /// // Use in an entity class
  /// public class User : Entity&lt;UserId&gt;
  /// {
  ///     public string Email { get; set; }
  ///     public string Name { get; set; }
  /// }
  /// 
  /// // Type-safe usage in methods
  /// public User GetUserById(UserId id) { /* implementation */ }
  /// </code>
  /// </example>
  [SuppressMessage(
    "Minor Code Smell",
    "S4023:Interfaces should not be empty",
    Justification = "Marker interface for Generic Constraint.")]
  public interface IStronglyTypedId { }
}
