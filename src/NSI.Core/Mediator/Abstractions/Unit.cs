using NSI.Core.Results;
namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Represents a unit type for requests that do not return a meaningful value.
/// </summary>
/// <remarks>
/// <para>
/// The Unit type serves as a functional programming concept that represents the absence of a 
/// meaningful return value, similar to <c>void</c> in synchronous contexts but compatible with 
/// generic type parameters. It enables consistent use of the <see cref="Result{T}"/> pattern 
/// across all <see cref="IRequestHandler{TRequest, TResponse}"/> implementations.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item><description>Value semantics: Implemented as a readonly record struct for optimal performance</description></item>
///   <item><description>Singleton pattern: Single instance accessed via <see cref="Value"/> property</description></item>
///   <item><description>Generic compatibility: Works seamlessly with Result&lt;T&gt; and async patterns</description></item>
///   <item><description>Functional programming: Represents successful completion without data</description></item>
/// </list>
/// </para>
/// <para>
/// Common usage patterns include:
/// <list type="bullet">
///   <item><description>Commands that perform actions without returning data (Create, Update, Delete operations)</description></item>
///   <item><description>Notifications in <see cref="INotification"/> implementations</description></item>
///   <item><description>Void-equivalent operations in async Result patterns</description></item>
///   <item><description>Pipeline operations that signal completion without payload</description></item>
/// </list>
/// </para>
/// <para>
/// Performance note: The Unit type is implemented as a readonly record struct with zero-cost 
/// abstraction, ensuring no runtime overhead compared to void operations while maintaining 
/// type safety and Result pattern consistency.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Command that doesn't return data
/// public record DeleteUserCommand(Guid UserId): ICommand&lt;Unit&gt;;
/// 
/// public class DeleteUserCommandHandler: IRequestHandler&lt;DeleteUserCommand, Unit&gt; {
///   private readonly IUserRepository _userRepository;
///   private readonly ILogger&lt;DeleteUserCommandHandler&gt; _logger;
///   
///   public DeleteUserCommandHandler(
///     IUserRepository userRepository, 
///     ILogger&lt;DeleteUserCommandHandler&gt; logger) {
///     _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
///     _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///   }
///   
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(
///     DeleteUserCommand request, 
///     CancellationToken cancellationToken = default) {
///     
///     ArgumentNullException.ThrowIfNull(request);
///     
///     _logger.LogUserDeletionStarted(request.UserId.ToString());
///     
///     var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
///     if (user is null) {
///       _logger.LogUserNotFound(request.UserId.ToString());
///       return Result.Failure&lt;Unit&gt;(ResultError.NotFound(
///         "USER_NOT_FOUND", 
///         $"User with ID {request.UserId} not found"));
///     }
///     
///     await _userRepository.DeleteAsync(user, cancellationToken);
///     await _userRepository.SaveChangesAsync(cancellationToken);
///     
///     _logger.LogUserDeletionCompleted(request.UserId.ToString());
///     
///     // Return Unit.Value to indicate successful completion
///     return Result.Success(Unit.Value);
///   }
/// }
/// 
/// // Notification example (inherits from INotification: IRequest&lt;Unit&gt;)
/// public record UserDeletedNotification(Guid UserId, DateTime DeletedAt): INotification;
/// 
/// public class UserDeletedNotificationHandler: IRequestHandler&lt;UserDeletedNotification, Unit&gt; {
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(
///     UserDeletedNotification request, 
///     CancellationToken cancellationToken = default) {
///     
///     // Send deletion confirmation email, update audit logs, etc.
///     await emailService.SendDeletionConfirmationAsync(request.UserId, cancellationToken);
///     
///     return Result.Success(Unit.Value);
///   }
/// }
/// 
/// // Usage in controller
/// [HttpDelete("{id}")]
/// public async Task&lt;IActionResult&gt; DeleteUser(Guid id, CancellationToken cancellationToken) {
///   var command = new DeleteUserCommand(id);
///   var result = await mediator.ProcessAsync(command, cancellationToken);
///   
///   return result.Match(
///     onSuccess: _ =&gt; NoContent(), // Unit value ignored, only success matters
///     onFailure: error =&gt; error.Type switch {
///       ErrorType.NotFound =&gt; NotFound(error.Message),
///       _ =&gt; StatusCode(500, "An error occurred")
///     }
///   );
/// }
/// 
/// // Comparison with non-Unit handler
/// public record GetUserQuery(Guid UserId): IQuery&lt;User&gt;; // Returns actual data
/// public record DeleteUserCommand(Guid UserId): ICommand&lt;Unit&gt;; // Returns only success/failure
/// </code>
/// </example>
/// <seealso cref="ICommand{TResponse}"/>
/// <seealso cref="INotification"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="Result{T}"/>
public readonly record struct Unit {
  /// <summary>
  /// Gets the singleton instance of the Unit type.
  /// </summary>
  /// <value>
  /// The single instance representing successful completion without data.
  /// </value>
  /// <remarks>
  /// This property provides the canonical instance used throughout the application
  /// to represent void-equivalent success states in the Result pattern.
  /// </remarks>
  public static readonly Unit Value = new();
}
