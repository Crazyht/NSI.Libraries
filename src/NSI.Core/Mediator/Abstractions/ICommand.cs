using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for commands that modify system state without returning a meaningful response.
/// </summary>
/// <remarks>
/// <para>
/// Commands without responses are used for operations where the success of the command execution
/// is sufficient confirmation, and no additional data needs to be returned to the caller.
/// These commands represent state-changing operations such as deletions, updates, or
/// fire-and-forget notifications where the operation result is binary (success or failure).
/// </para>
/// <para>
/// Architectural benefits and usage patterns:
/// <list type="bullet">
///   <item><description>Simplified command definitions - no need to specify generic return types</description></item>
///   <item><description>Consistent with CQRS patterns - commands modify state, queries return data</description></item>
///   <item><description>Maintains Result pattern consistency by returning Unit.Value for success scenarios</description></item>
///   <item><description>Clear semantic distinction from ICommand&lt;TResponse&gt; variants</description></item>
///   <item><description>Reduces cognitive overhead for operations that don't need return values</description></item>
/// </list>
/// </para>
/// <para>
/// Implementation guidance: Commands implementing this interface should represent atomic operations
/// that either succeed completely or fail with appropriate error information. The lack of a return
/// value should not be confused with lack of validation - commands should still perform thorough
/// input validation and return meaningful error results when validation fails.
/// </para>
/// <para>
/// Integration with mediator pattern: This interface works seamlessly with the mediator pattern
/// by inheriting from <see cref="ICommand{TResponse}"/> with <see cref="Unit"/> as the response type.
/// Handlers should return <see cref="Result{T}"/> where T is <see cref="Unit"/> to maintain
/// consistency with the error handling and success patterns used throughout the system.
/// </para>
/// <para>
/// Thread Safety: Command instances should be immutable and stateless, making them inherently
/// thread-safe. All mutable state should be encapsulated within the command handler implementation.
/// </para>
/// </remarks>
/// <example>
/// Basic command implementations:
/// <code>
/// // Simple deletion command
/// public sealed record DeleteUserCommand(UserId UserId) : ICommand;
/// 
/// // Complex state modification command
/// public sealed record ArchiveOrderCommand(
///   OrderId OrderId,
///   string Reason,
///   DateTime ArchiveDate
/// ) : ICommand;
/// 
/// // Notification command without return data
/// public sealed record SendWelcomeEmailCommand(
///   UserId UserId,
///   string EmailAddress,
///   string UserName
/// ) : ICommand;
/// </code>
/// 
/// Command handler implementation:
/// <code>
/// public sealed class DeleteUserCommandHandler : IRequestHandler&lt;DeleteUserCommand, Unit&gt; {
///   private readonly IUserRepository _userRepository;
///   private readonly ILogger&lt;DeleteUserCommandHandler&gt; _logger;
///   
///   public DeleteUserCommandHandler(
///     IUserRepository userRepository,
///     ILogger&lt;DeleteUserCommandHandler&gt; logger) {
///     _userRepository = userRepository;
///     _logger = logger;
///   }
///   
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(
///     DeleteUserCommand request,
///     CancellationToken cancellationToken) {
///     
///     // Validate command
///     if (request.UserId == UserId.Empty) {
///       return Result.Failure&lt;Unit&gt;(
///         ValidationErrors.Required(nameof(request.UserId)));
///     }
///     
///     // Execute deletion
///     var deleteResult = await _userRepository.DeleteAsync(
///       request.UserId, cancellationToken);
///       
///     if (!deleteResult.IsSuccess) {
///       _logger.LogUserDeletionFailed(request.UserId.ToString(), deleteResult.Error.Message);
///       return Result.Failure&lt;Unit&gt;(deleteResult.Error);
///     }
///     
///     _logger.LogUserDeleted(request.UserId.ToString());
///     return Result.Success(Unit.Value);
///   }
/// }
/// </code>
/// 
/// Usage in controllers or services:
/// <code>
/// [HttpDelete("users/{userId}")]
/// public async Task&lt;ActionResult&gt; DeleteUser([FromRoute] Guid userId) {
///   var command = new DeleteUserCommand(new UserId(userId));
///   var result = await _mediator.SendAsync(command);
///   
///   return result.Match(
///     onSuccess: _ => NoContent(),
///     onFailure: error => BadRequest(error)
///   );
/// }
/// </code>
/// 
/// Validation and business rules:
/// <code>
/// public sealed class ArchiveOrderCommandHandler : IRequestHandler&lt;ArchiveOrderCommand, Unit&gt; {
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(
///     ArchiveOrderCommand request,
///     CancellationToken cancellationToken) {
///     
///     // Complex validation
///     var validationResult = await ValidateArchiveRequest(request);
///     if (!validationResult.IsSuccess) {
///       return Result.Failure&lt;Unit&gt;(validationResult.Error);
///     }
///     
///     // Execute archival process
///     await ExecuteArchival(request, cancellationToken);
///     
///     return Result.Success(Unit.Value);
///   }
///   
///   private async Task&lt;Result&lt;Unit&gt;&gt; ValidateArchiveRequest(ArchiveOrderCommand command) {
///     // Business rule validation
///     if (command.ArchiveDate &gt; DateTime.UtcNow) {
///       return Result.Failure&lt;Unit&gt;(
///         ValidationErrors.FutureDate(nameof(command.ArchiveDate)));
///     }
///     
///     // Additional validation logic...
///     return Result.Success(Unit.Value);
///   }
/// }
/// </code>
/// </example>
public interface ICommand: ICommand<Unit> { }
