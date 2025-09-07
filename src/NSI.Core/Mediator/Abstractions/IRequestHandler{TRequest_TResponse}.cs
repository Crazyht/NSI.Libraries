using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions {
  /// <summary>
  /// Defines a handler for a request that returns a response.
  /// </summary>
  /// <typeparam name="TRequest">The type of request being handled.</typeparam>
  /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
  /// <remarks>
  /// <para>
  /// Request handlers contain the business logic for processing specific request types.
  /// Each handler should be focused on a single responsibility and follow the
  /// Single Responsibility Principle.
  /// </para>
  /// <para>
  /// Handlers should be stateless or thread-safe to support concurrent execution.
  /// Dependencies should be injected through the constructor and stored in readonly fields.
  /// </para>
  /// <para>
  /// The Result pattern is used to handle both successful operations and errors
  /// without throwing exceptions for business logic failures.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public class GetUserByIdQueryHandler : IRequestHandler&lt;GetUserByIdQuery, User&gt; {
  ///   private readonly IUserRepository _userRepository;
  ///   private readonly ILogger&lt;GetUserByIdQueryHandler&gt; _logger;
  ///   
  ///   public GetUserByIdQueryHandler(IUserRepository userRepository, ILogger&lt;GetUserByIdQueryHandler&gt; logger) {
  ///     _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
  ///     _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  ///   }
  ///   
  ///   public async Task&lt;Result&lt;User&gt;&gt; HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken = default) {
  ///     ArgumentNullException.ThrowIfNull(request);
  ///     
  ///     _logger.LogDebug("Retrieving user with ID: {UserId}", request.UserId);
  ///     
  ///     var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
  ///     
  ///     if (user is null) {
  ///       return Result.Failure&lt;User&gt;(ResultError.NotFound(
  ///         "USER_NOT_FOUND", 
  ///         $"User with ID {request.UserId} was not found"));
  ///     }
  ///     
  ///     return Result.Success(user);
  ///   }
  /// }
  /// </code>
  /// </example>
  public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse> {

    /// <summary>
    /// Handles a request asynchronously.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a Result with either the response value or error information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <remarks>
    /// <para>
    /// Implementations should:
    /// <list type="bullet">
    ///   <item><description>Validate the request parameter for null</description></item>
    ///   <item><description>Respect the cancellation token throughout the operation</description></item>
    ///   <item><description>Return Result.Success for successful operations</description></item>
    ///   <item><description>Return Result.Failure for business logic errors</description></item>
    ///   <item><description>Let infrastructure exceptions bubble up (they will be handled by the mediator)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
  }
}
