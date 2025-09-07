using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator.HealthChecks;
/// <summary>
/// Simple health check query for testing mediator functionality.
/// </summary>
/// <remarks>
/// This query is used internally by the health check system and should not
/// be used in application code.
/// </remarks>
public record MediatorHealthCheckQuery: IQuery<string>;
