using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Defines a handler for a request that returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// Request handlers implement the Command Handler and Query Handler patterns as part of the
/// CQRS (Command Query Responsibility Segregation) architecture. Each handler contains the
/// core business logic for processing a specific request type and should follow the
/// Single Responsibility Principle.
/// </para>
/// <para>
/// Key architectural principles:
/// <list type="bullet">
///   <item><description>Single Responsibility: Each handler processes exactly one request type</description></item>
///   <item><description>Stateless design: Handlers should be stateless or thread-safe for concurrent execution</description></item>
///   <item><description>Dependency Injection: Dependencies injected through constructor and stored in readonly fields</description></item>
///   <item><description>Result pattern: Use <see cref="Result{T}"/> to handle success and business failures without exceptions</description></item>
///   <item><description>Infrastructure exceptions: Let infrastructure exceptions bubble up for mediator handling</description></item>
/// </list>
/// </para>
/// <para>
/// Handlers are executed by <see cref="IMediator.ProcessAsync{TResponse}"/> and may be wrapped
/// by <see cref="IRequestDecorator{TRequest, TResponse}"/> implementations to provide
/// cross-cutting concerns like logging, validation, caching, and authorization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query handler example with high-performance logging
/// public class GetUserByIdQueryHandler: IRequestHandler&lt;GetUserByIdQuery, User&gt; {
///   private readonly IUserRepository _userRepository;
///   private readonly ILogger&lt;GetUserByIdQueryHandler&gt; _logger;
///   
///   public GetUserByIdQueryHandler(
///     IUserRepository userRepository, 
///     ILogger&lt;GetUserByIdQueryHandler&gt; logger) {
///     _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
///     _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///   }
///   
///   public async Task&lt;Result&lt;User&gt;&gt; HandleAsync(
///     GetUserByIdQuery request, 
///     CancellationToken cancellationToken = default) {
///     
///     ArgumentNullException.ThrowIfNull(request);
///     
///     _logger.LogQueryProcessingStarted(nameof(GetUserByIdQuery), request.UserId.ToString());
///     
///     try {
///       var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
///       
///       if (user is null) {
///         _logger.LogUserNotFound(request.UserId.ToString());
///         return Result.Failure&lt;User&gt;(ResultError.NotFound(
///           "USER_NOT_FOUND", 
///           $"User with ID {request.UserId} was not found"));
///       }
///       
///       _logger.LogQueryProcessingCompleted(nameof(GetUserByIdQuery), request.UserId.ToString());
///       return Result.Success(user);
///     }
///     catch (Exception ex) when (ex is not OperationCanceledException) {
///       _logger.LogQueryProcessingFailed(nameof(GetUserByIdQuery), request.UserId.ToString(), ex);
///       throw; // Let infrastructure exceptions bubble up
///     }
///   }
/// }
/// 
/// // Command handler example with validation and business logic
/// public class CreateUserCommandHandler: IRequestHandler&lt;CreateUserCommand, User&gt; {
///   private readonly IUserRepository _userRepository;
///   private readonly IEmailService _emailService;
///   private readonly ILogger&lt;CreateUserCommandHandler&gt; _logger;
///   
///   public CreateUserCommandHandler(
///     IUserRepository userRepository,
///     IEmailService emailService,
///     ILogger&lt;CreateUserCommandHandler&gt; logger) {
///     _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
///     _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
///     _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///   }
///   
///   public async Task&lt;Result&lt;User&gt;&gt; HandleAsync(
///     CreateUserCommand request, 
///     CancellationToken cancellationToken = default) {
///     
///     ArgumentNullException.ThrowIfNull(request);
///     
///     _logger.LogCommandProcessingStarted(nameof(CreateUserCommand), request.Email);
///     
///     // Check for existing user (business rule)
///     var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
///     if (existingUser is not null) {
///       _logger.LogUserAlreadyExists(request.Email);
///       return Result.Failure&lt;User&gt;(ResultError.Conflict(
///         "USER_ALREADY_EXISTS",
///         $"User with email {request.Email} already exists"));
///     }
///     
///     // Create and save user
///     var user = new User {
///       Id = Guid.NewGuid(),
///       Email = request.Email,
///       Name = request.Name,
///       CreatedAt = DateTime.UtcNow
///     };
///     
///     await _userRepository.AddAsync(user, cancellationToken);
///     await _userRepository.SaveChangesAsync(cancellationToken);
///     
///     _logger.LogCommandProcessingCompleted(nameof(CreateUserCommand), user.Id.ToString());
///     return Result.Success(user);
///   }
/// }
/// 
/// // Usage in dependency injection
/// services.AddScoped&lt;IRequestHandler&lt;GetUserByIdQuery, User&gt;, GetUserByIdQueryHandler&gt;();
/// services.AddScoped&lt;IRequestHandler&lt;CreateUserCommand, User&gt;, CreateUserCommandHandler&gt;();
/// </code>
/// </example>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequest{TResponse}"/>
/// <seealso cref="IQuery{TResponse}"/>
/// <seealso cref="ICommand{TResponse}"/>
/// <seealso cref="IRequestDecorator{TRequest, TResponse}"/>
/// <seealso cref="Result{T}"/>
/// <seealso cref="ResultError"/>
public interface IRequestHandler<in TRequest, TResponse>
  where TRequest : IRequest<TResponse> {

  /// <summary>
  /// Handles a request asynchronously.
  /// </summary>
  /// <param name="request">
  /// The request to handle. Must not be null and must implement <see cref="IRequest{TResponse}"/>.
  /// </param>
  /// <param name="cancellationToken">
  /// A cancellation token that can be used to cancel the operation.
  /// Implementations should respect this token and pass it to all async operations.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous operation.
  /// The task result contains a <see cref="Result{T}"/> with either the response value or error information.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// Implementations must follow these guidelines:
  /// <list type="bullet">
  ///   <item><description>Validate parameters using <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/></description></item>
  ///   <item><description>Respect the <paramref name="cancellationToken"/> and pass it to all async operations</description></item>
  ///   <item><description>Return <see cref="Result.Success{T}(T)"/> for successful operations</description></item>
  ///   <item><description>Return <see cref="Result.Failure{T}(ResultError)"/> for business logic errors</description></item>
  ///   <item><description>Let infrastructure exceptions bubble up (they will be handled by the mediator)</description></item>
  ///   <item><description>Use high-performance logging with LoggerMessage source generators</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Error handling strategy:
  /// <list type="bullet">
  ///   <item><description>Business failures: Return Result.Failure with appropriate <see cref="ResultError"/></description></item>
  ///   <item><description>Validation errors: Use ResultError.Validation factory methods with validation error collections</description></item>
  ///   <item><description>Not found scenarios: Use <see cref="ResultError.NotFound(string, string, Exception?)"/></description></item>
  ///   <item><description>Authentication failures: Use <see cref="ResultError.Unauthorized(string, string, Exception?)"/></description></item>
  ///   <item><description>Authorization failures: Use <see cref="ResultError.Forbidden(string, string, Exception?)"/></description></item>
  ///   <item><description>Conflict scenarios: Use <see cref="ResultError.Conflict(string, string, Exception?)"/></description></item>
  ///   <item><description>Business rule violations: Use <see cref="ResultError.BusinessRule(string, string, Exception?)"/></description></item>
  ///   <item><description>Database errors: Use <see cref="ResultError.Database(string, string, Exception?)"/></description></item>
  ///   <item><description>Service unavailable: Use <see cref="ResultError.ServiceUnavailable(string, string, Exception?)"/></description></item>
  ///   <item><description>Infrastructure exceptions: Let them propagate to be handled by decorators or mediator</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Performance considerations: Handlers should be stateless and thread-safe since they
  /// may be registered as scoped or singleton services. Use readonly fields for dependencies
  /// and avoid shared mutable state.
  /// </para>
  /// </remarks>
  public Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
