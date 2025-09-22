namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for read-only queries that retrieve data without side effects.
/// </summary>
/// <typeparam name="TResponse">The type of the data being queried.</typeparam>
/// <remarks>
/// <para>
/// Queries represent the read side of the CQRS (Command Query Responsibility Segregation) pattern.
/// They should be idempotent and have no observable side effects on the system state.
/// Queries are used exclusively to retrieve data and must not modify any persistent state.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item><description>Idempotent: Multiple executions produce the same result</description></item>
///   <item><description>Side-effect free: No modifications to system state</description></item>
///   <item><description>Read-optimized: Can use specialized read models or projections</description></item>
///   <item><description>Cacheable: Results can be cached since they don't change state</description></item>
/// </list>
/// </para>
/// <para>
/// Queries are processed by <see cref="IMediator.ProcessAsync{TResponse}"/> and handled by
/// implementations of <see cref="IRequestHandler{TRequest, TResponse}"/> where TRequest
/// implements IQuery&lt;TResponse&gt;.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple entity retrieval query
/// public record GetUserByIdQuery(Guid Id): IQuery&lt;User&gt;;
/// 
/// // Paged query with filtering
/// public record GetUsersPagedQuery(int Page, int Size, string? Filter = null): IQuery&lt;PagedResult&lt;User&gt;&gt;;
/// 
/// // Complex search query with multiple criteria
/// public record SearchProductsQuery(
///   string SearchTerm, 
///   decimal? MinPrice = null, 
///   decimal? MaxPrice = null,
///   ProductCategory? Category = null): IQuery&lt;IEnumerable&lt;Product&gt;&gt;;
/// 
/// // Handler implementation example
/// public class GetUserByIdHandler: IRequestHandler&lt;GetUserByIdQuery, User&gt; {
///   public async Task&lt;Result&lt;User&gt;&gt; HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken) {
///     var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
///     return user is not null 
///       ? Result.Success(user)
///       : Result.Failure&lt;User&gt;(new ResultError(ErrorType.NotFound, "USER_NOT_FOUND", $"User with ID {request.Id} not found."));
///   }
/// }
/// 
/// // Usage in controller
/// [HttpGet("{id}")]
/// public async Task&lt;IActionResult&gt; GetUser(Guid id, CancellationToken cancellationToken) {
///   var query = new GetUserByIdQuery(id);
///   var result = await mediator.ProcessAsync(query, cancellationToken);
///   
///   return result.Match(
///     onSuccess: user =&gt; Ok(user),
///     onFailure: error =&gt; error.Type switch {
///       ErrorType.NotFound =&gt; NotFound(error.Message),
///       _ =&gt; StatusCode(500, "An error occurred")
///     }
///   );
/// }
/// </code>
/// </example>
/// <seealso cref="ICommand{TResponse}"/>
/// <seealso cref="IRequest{TResponse}"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
public interface IQuery<TResponse>: IRequest<TResponse> { }
