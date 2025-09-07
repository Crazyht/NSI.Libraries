namespace NSI.Core.Mediator.Abstractions;
/// <summary>
/// Marker interface for read-only queries that retrieve data without side effects.
/// </summary>
/// <typeparam name="TResponse">The type of the data being queried.</typeparam>
/// <remarks>
/// <para>
/// Queries should be idempotent and have no observable side effects on the system state.
/// They are used to retrieve data and should not modify any persistent state.
/// </para>
/// <para>
/// Following CQRS principles, queries should be optimized for read operations
/// and may use different data stores or models than commands.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record GetUserByIdQuery(Guid Id) : IQuery&lt;User&gt;;
/// public record GetUsersPagedQuery(int Page, int Size, string? Filter = null) : IQuery&lt;PagedResult&lt;User&gt;&gt;;
/// public record SearchProductsQuery(string SearchTerm, decimal? MinPrice, decimal? MaxPrice) : IQuery&lt;IEnumerable&lt;Product&gt;&gt;;
/// </code>
/// </example>
public interface IQuery<TResponse>: IRequest<TResponse> { }
