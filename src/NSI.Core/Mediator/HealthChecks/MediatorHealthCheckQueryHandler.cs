using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;

namespace NSI.Core.Mediator.HealthChecks {
  /// <summary>
  /// Handler for the mediator health check query.
  /// </summary>
  /// <remarks>
  /// This handler is automatically registered when using the health check extensions.
  /// </remarks>
  public class MediatorHealthCheckQueryHandler: IRequestHandler<MediatorHealthCheckQuery, string> {
    /// <summary>
    /// Handles the health check query.
    /// </summary>
    /// <param name="request">The health check query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result with a simple message.</returns>
    public Task<Result<string>> HandleAsync(MediatorHealthCheckQuery request, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success("Mediator is healthy"));
  }
}
