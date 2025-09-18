namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for commands that modify system state and return a meaningful response.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned after successful command execution.</typeparam>
/// <remarks>
/// <para>
/// Commands with responses represent state-changing operations that need to return meaningful
/// data to the caller after successful execution. These commands are particularly useful when
/// the caller needs access to the created, modified, or processed entity, or when confirmation
/// data about the operation is required for subsequent business logic.
/// </para>
/// <para>
/// Architectural patterns and design principles:
/// <list type="bullet">
///   <item><description>CQRS compliance - commands modify state, queries retrieve data</description></item>
///   <item><description>Mediator pattern integration - works seamlessly with IMediator implementations</description></item>
///   <item><description>Result pattern compatibility - handlers return Result&lt;TResponse&gt; for error handling</description></item>
///   <item><description>Strong typing - compile-time safety for response types</description></item>
///   <item><description>Immutable command design - commands should be read-only value objects</description></item>
/// </list>
/// </para>
/// <para>
/// Usage scenarios and best practices: Commands implementing this interface should represent
/// atomic business operations that either succeed completely (returning the specified response)
/// or fail with appropriate error information. The response type should contain only the data
/// necessary for the caller, avoiding over-fetching or exposing sensitive information.
/// </para>
/// <para>
/// Integration with request pipeline: This interface extends <see cref="IRequest{TResponse}"/>
/// to leverage existing mediator infrastructure while maintaining semantic clarity about the
/// command's intent. Handlers should implement <see cref="IRequestHandler{TRequest, TResponse}"/>
/// where TRequest is the command and TResponse matches the command's generic parameter.
/// </para>
/// <para>
/// Thread Safety: Command instances should be immutable and stateless, making them inherently
/// thread-safe. All mutable state should be encapsulated within the command handler implementation,
/// and handlers should be designed to handle concurrent execution appropriately.
/// </para>
/// <para>
/// Performance considerations: Response objects should be lightweight and focused on essential
/// data. For large result sets or complex entities, consider using projection patterns or
/// pagination to minimize memory usage and serialization overhead.
/// </para>
/// </remarks>
/// <example>
/// Basic command implementations with different response types:
/// <code>
/// // Command returning created entity
/// public sealed record CreateUserCommand(
///   string Email,
///   string FirstName,
///   string LastName
/// ) : ICommand&lt;User&gt;;
/// 
/// // Command returning modified entity with additional metadata
/// public sealed record UpdateUserCommand(
///   UserId UserId,
///   string FirstName,
///   string LastName,
///   int Version
/// ) : ICommand&lt;UserUpdateResult&gt;;
/// 
/// // Command returning operation result with confirmation data
/// public sealed record ProcessPaymentCommand(
///   OrderId OrderId,
///   PaymentDetails PaymentDetails,
///   PaymentMethod PaymentMethod
/// ) : ICommand&lt;PaymentProcessingResult&gt;;
/// 
/// // Command returning summary statistics
/// public sealed record ImportDataCommand(
///   Stream DataStream,
///   ImportOptions Options
/// ) : ICommand&lt;ImportSummary&gt;;
/// </code>
/// 
/// Command handler implementation:
/// <code>
/// public sealed class CreateUserCommandHandler 
///   : IRequestHandler&lt;CreateUserCommand, User&gt; {
///   
///   private readonly IUserRepository _userRepository;
///   private readonly IValidator&lt;CreateUserCommand&gt; _validator;
///   private readonly ILogger&lt;CreateUserCommandHandler&gt; _logger;
///   
///   public CreateUserCommandHandler(
///     IUserRepository userRepository,
///     IValidator&lt;CreateUserCommand&gt; validator,
///     ILogger&lt;CreateUserCommandHandler&gt; logger) {
///     _userRepository = userRepository;
///     _validator = validator;
///     _logger = logger;
///   }
///   
///   public async Task&lt;Result&lt;User&gt;&gt; HandleAsync(
///     CreateUserCommand request,
///     CancellationToken cancellationToken) {
///     
///     // Validate command
///     var validationResult = await _validator.ValidateAsync(request);
///     if (!validationResult.IsValid) {
///       _logger.LogUserCreationValidationFailed(request.Email, validationResult.ErrorMessage);
///       return Result.Failure&lt;User&gt;(validationResult.ToError());
///     }
///     
///     // Check business rules
///     var existingUser = await _userRepository.GetByEmailAsync(
///       request.Email, cancellationToken);
///     if (existingUser != null) {
///       return Result.Failure&lt;User&gt;(
///         UserErrors.EmailAlreadyExists(request.Email));
///     }
///     
///     // Create new user
///     var user = User.Create(
///       request.Email,
///       request.FirstName,
///       request.LastName);
///       
///     var createResult = await _userRepository.CreateAsync(user, cancellationToken);
///     if (!createResult.IsSuccess) {
///       _logger.LogUserCreationFailed(request.Email, createResult.Error.Message);
///       return Result.Failure&lt;User&gt;(createResult.Error);
///     }
///     
///     _logger.LogUserCreated(user.Id.ToString(), user.Email);
///     return Result.Success(user);
///   }
/// }
/// </code>
/// 
/// Usage in controllers with proper error handling:
/// <code>
/// [ApiController]
/// [Route("api/[controller]")]
/// public sealed class UsersController : ControllerBase {
///   private readonly IMediator _mediator;
///   
///   public UsersController(IMediator mediator) => _mediator = mediator;
///   
///   [HttpPost]
///   public async Task&lt;ActionResult&lt;UserDto&gt;&gt; CreateUser(
///     [FromBody] CreateUserRequest request) {
///     
///     var command = new CreateUserCommand(
///       request.Email,
///       request.FirstName,
///       request.LastName);
///       
///     var result = await _mediator.SendAsync(command);
///     
///     return result.Match(
///       onSuccess: user => CreatedAtAction(
///         nameof(GetUser),
///         new { id = user.Id },
///         user.ToDto()),
///       onFailure: error => BadRequest(error.ToApiError())
///     );
///   }
/// }
/// </code>
/// 
/// Advanced scenarios with complex response types:
/// <code>
/// // Complex response with metadata
/// public sealed record UserUpdateResult(
///   User UpdatedUser,
///   DateTime LastModified,
///   string ModifiedBy,
///   int NewVersion
/// );
/// 
/// // Batch operation response
/// public sealed record ImportSummary(
///   int TotalProcessed,
///   int SuccessfulImports,
///   int FailedImports,
///   IReadOnlyList&lt;ImportError&gt; Errors,
///   TimeSpan ProcessingTime
/// );
/// 
/// // Payment processing response
/// public sealed record PaymentProcessingResult(
///   PaymentStatus Status,
///   string TransactionId,
///   decimal ProcessedAmount,
///   Currency Currency,
///   DateTime ProcessedAt,
///   PaymentMethod PaymentMethod
/// );
/// </code>
/// 
/// Error handling and validation patterns:
/// <code>
/// public sealed class UpdateUserCommandHandler 
///   : IRequestHandler&lt;UpdateUserCommand, UserUpdateResult&gt; {
///   
///   public async Task&lt;Result&lt;UserUpdateResult&gt;&gt; HandleAsync(
///     UpdateUserCommand request,
///     CancellationToken cancellationToken) {
///     
///     // Optimistic concurrency check
///     var user = await _userRepository.GetByIdAsync(
///       request.UserId, cancellationToken);
///       
///     if (user == null) {
///       return Result.Failure&lt;UserUpdateResult&gt;(
///         UserErrors.NotFound(request.UserId));
///     }
///     
///     if (user.Version != request.Version) {
///       return Result.Failure&lt;UserUpdateResult&gt;(
///         UserErrors.ConcurrencyConflict(request.UserId, request.Version));
///     }
///     
///     // Apply updates
///     user.UpdateDetails(request.FirstName, request.LastName);
///     
///     var updateResult = await _userRepository.UpdateAsync(user, cancellationToken);
///     if (!updateResult.IsSuccess) {
///       return Result.Failure&lt;UserUpdateResult&gt;(updateResult.Error);
///     }
///     
///     // Return comprehensive result
///     return Result.Success(new UserUpdateResult(
///       UpdatedUser: user,
///       LastModified: DateTime.UtcNow,
///       ModifiedBy: _currentUser.Username,
///       NewVersion: user.Version
///     ));
///   }
/// }
/// </code>
/// </example>
public interface ICommand<TResponse>: IRequest<TResponse> { }
