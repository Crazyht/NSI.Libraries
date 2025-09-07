using System.Diagnostics.CodeAnalysis;

namespace NSI.Core.Mediator.Abstractions {
  /// <summary>
  /// Marker interface for all requests that return a response.
  /// </summary>
  /// <typeparam name="TResponse">The type of the response.</typeparam>
  /// <remarks>
  /// <para>
  /// This is the base interface for all request types in the mediator pattern.
  /// It provides type safety by linking requests with their expected response types.
  /// </para>
  /// <para>
  /// Implementations should be immutable record types that contain all the data
  /// needed to process the request.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public record GetUserByIdQuery(Guid UserId) : IQuery&lt;User&gt;;
  /// public record CreateUserCommand(string Email, string Name) : ICommand&lt;User&gt;;
  /// </code>
  /// </example>
  [SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "Used to link Response type with request.")]
  public interface IRequest<TResponse> { }
}
