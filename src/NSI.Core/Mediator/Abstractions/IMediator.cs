using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions;
/// <summary>
/// Defines the central dispatcher for application requests and notifications.
/// </summary>
/// <remarks>
/// <para>
/// The mediator acts as a central hub that routes requests to their appropriate handlers,
/// implements cross-cutting concerns through decorators, and manages the execution pipeline.
/// </para>
/// <para>
/// Key responsibilities:
/// <list type="bullet">
///   <item><description>Route requests to appropriate handlers</description></item>
///   <item><description>Apply cross-cutting concerns (logging, validation, etc.)</description></item>
///   <item><description>Handle errors and convert exceptions to Results</description></item>
///   <item><description>Support fire-and-forget notifications</description></item>
/// </list>
/// </para>
/// <para>
/// The mediator should be registered as a scoped service in the dependency injection
/// container to ensure proper handler lifetime management.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a controller
/// [ApiController]
/// public class UsersController : ControllerBase {
///   private readonly IMediator _mediator;
///   
///   public UsersController(IMediator mediator) {
///     _mediator = mediator;
///   }
///   
///   [HttpGet("{id}")]
///   public async Task&lt;IActionResult&gt; GetUser(Guid id, CancellationToken cancellationToken) {
///     var query = new GetUserByIdQuery(id);
///     var result = await _mediator.ProcessAsync(query, cancellationToken);
///     
///     return result.Match(
///       onSuccess: user =&gt; Ok(user),
///       onFailure: error =&gt; error.Type switch {
///         ErrorType.NotFound =&gt; NotFound(error.Message),
///         ErrorType.Validation =&gt; BadRequest(error.ValidationErrors),
///         _ =&gt; StatusCode(500, "An error occurred")
///       }
///     );
///   }
///   
///   [HttpPost]
///   public async Task&lt;IActionResult&gt; CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken) {
///     var result = await _mediator.ProcessAsync(command, cancellationToken);
///     
///     if (result.IsSuccess) {
///       // Fire notification without waiting
///       await _mediator.DispatchAsync(new UserCreatedNotification(result.Value.Id, result.Value.Email, DateTime.UtcNow));
///       return CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value);
///     }
///     
///     return BadRequest(result.Error);
///   }
/// }
/// </code>
/// </example>
public interface IMediator {
  /// <summary>
  /// Processes a request and returns a typed response.
  /// </summary>
  /// <typeparam name="TResponse">The type of the expected response.</typeparam>
  /// <param name="request">The request to process.</param>
  /// <param name="cancellationToken">
  /// A cancellation token that can be used to cancel the operation.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous operation.
  /// The task result contains a Result with either the response or error information.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
  /// <remarks>
  /// <para>
  /// This method:
  /// <list type="bullet">
  ///   <item><description>Resolves the appropriate handler for the request type</description></item>
  ///   <item><description>Applies any registered decorators in the correct order</description></item>
  ///   <item><description>Executes the handler and returns the result</description></item>
  ///   <item><description>Converts any infrastructure exceptions to Result.Failure</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// If no handler is found for the request type, returns a Result.Failure with
  /// ErrorType.NotFound and code "HANDLER_NOT_FOUND".
  /// </para>
  /// <para>
  /// The generic constraint ensures type safety while allowing for automatic type inference
  /// when calling the method with concrete request instances.
  /// </para>
  /// </remarks>
  public Task<Result<TResponse>> ProcessAsync<TResponse>(
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Dispatches a notification to all registered handlers without waiting for completion.
  /// </summary>
  /// <param name="notification">The notification to dispatch.</param>
  /// <param name="cancellationToken">
  /// A cancellation token that can be used to cancel the operation.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous operation.
  /// The task completes when all handlers have been started (not when they complete).
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when notification is null.</exception>
  /// <remarks>
  /// <para>
  /// This method implements fire-and-forget semantics:
  /// <list type="bullet">
  ///   <item><description>All handlers are executed in parallel</description></item>
  ///   <item><description>Failure of one handler doesn't affect others</description></item>
  ///   <item><description>The method returns as soon as all handlers are started</description></item>
  ///   <item><description>Exceptions in handlers are logged but not propagated</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// If no handlers are registered for the notification type, the method completes
  /// successfully without any action.
  /// </para>
  /// <para>
  /// The generic constraint ensures type safety while allowing for automatic type inference
  /// when calling the method with concrete notification instances.
  /// </para>
  /// </remarks>
  public Task DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
    where TNotification : INotification;
}
