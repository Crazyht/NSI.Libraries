namespace NSI.Core.Mediator.Abstractions;
/// <summary>
/// Marker interface for commands that modify system state without returning a meaningful response.
/// </summary>
/// <remarks>
/// <para>
/// Commands without responses are used for operations where the success of the command
/// is sufficient confirmation, and no additional data needs to be returned.
/// </para>
/// <para>
/// These commands return Unit.Value to maintain consistency with the generic Result pattern
/// while avoiding the need for a meaningful return value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record DeleteUserCommand(Guid UserId) : ICommand;
/// public record SendEmailCommand(string To, string Subject, string Body) : ICommand;
/// public record ArchiveOrderCommand(Guid OrderId) : ICommand;
/// </code>
/// </example>
public interface ICommand: ICommand<Unit> { }
