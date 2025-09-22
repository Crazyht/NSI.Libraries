using System.Diagnostics.CodeAnalysis;

namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for all requests that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// <para>
/// This is the foundational interface for all request types in the mediator pattern.
/// It provides compile-time type safety by linking requests with their expected response types,
/// enabling the mediator to resolve the correct handler and ensure type consistency throughout
/// the request processing pipeline.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item><description>Type safety: Links request types with their response types at compile time</description></item>
///   <item><description>Handler resolution: Enables automatic handler discovery by the mediator</description></item>
///   <item><description>Pipeline support: Works with decorators for cross-cutting concerns</description></item>
///   <item><description>Immutable design: Implementations should be immutable record types</description></item>
/// </list>
/// </para>
/// <para>
/// Requests are processed by <see cref="IMediator.ProcessAsync{TResponse}"/> and handled by
/// implementations of <see cref="IRequestHandler{TRequest, TResponse}"/>. The mediator uses
/// the generic type parameter to resolve the appropriate handler and ensure type safety.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query request (read-only operation)
/// public record GetUserByIdQuery(Guid UserId): IQuery&lt;User&gt;;
/// 
/// // Command request (write operation)
/// public record CreateUserCommand(string Email, string Name): ICommand&lt;User&gt;;
/// 
/// // Complex request with multiple parameters
/// public record UpdateUserProfileCommand(
///   Guid UserId, 
///   string? NewName = null, 
///   string? NewEmail = null): ICommand&lt;User&gt;;
/// 
/// // Handler implementation example
/// public class GetUserByIdHandler: IRequestHandler&lt;GetUserByIdQuery, User&gt; {
///   public async Task&lt;Result&lt;User&gt;&gt; HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken) {
///     var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
///     return user is not null 
///       ? Result.Success(user)
///       : Result.Failure&lt;User&gt;(new ResultError(ErrorType.NotFound, "USER_NOT_FOUND", $"User with ID {request.UserId} not found."));
///   }
/// }
/// 
/// // Usage in controller
/// [HttpPost]
/// public async Task&lt;IActionResult&gt; CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken) {
///   var result = await mediator.ProcessAsync(command, cancellationToken);
///   
///   return result.Match(
///     onSuccess: user =&gt; CreatedAtAction(nameof(GetUser), new { id = user.Id }, user),
///     onFailure: error =&gt; error.Type switch {
///       ErrorType.Validation =&gt; BadRequest(error.ValidationErrors),
///       _ =&gt; StatusCode(500, "An error occurred")
///     }
///   );
/// }
/// </code>
/// </example>
/// <seealso cref="IQuery{TResponse}"/>
/// <seealso cref="ICommand{TResponse}"/>
/// <seealso cref="INotification"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
[SuppressMessage(
  "Major Code Smell",
  "S2326:Unused type parameters should be removed",
  Justification = "Used to link Response type with request.")]
public interface IRequest<TResponse> { }
