
namespace NSI.Core.Mediator.Abstractions {
  /// <summary>
  /// Represents a unit type for requests that do not return a value.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The Unit type is used to represent the absence of a meaningful return value,
  /// similar to void in synchronous contexts but compatible with generic type parameters.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public record DeleteUserCommand(Guid UserId) : ICommand;
  /// 
  /// public class DeleteUserCommandHandler : IRequestHandler&lt;DeleteUserCommand, Unit&gt; {
  ///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken) {
  ///     // Delete user logic
  ///     return Result.Success(Unit.Value);
  ///   }
  /// }
  /// </code>
  /// </example>
  public readonly record struct Unit {
    /// <summary>
    /// Gets the singleton instance of the Unit type.
    /// </summary>
    public static readonly Unit Value = new();
  }
}
