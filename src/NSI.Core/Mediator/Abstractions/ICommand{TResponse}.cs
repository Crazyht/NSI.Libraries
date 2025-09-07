namespace NSI.Core.Mediator.Abstractions {
  /// <summary>
  /// Marker interface for commands that modify system state and return a response.
  /// </summary>
  /// <typeparam name="TResponse">The type of the response after command execution.</typeparam>
  /// <remarks>
  /// <para>
  /// Commands represent operations that change the system state. They may have side effects
  /// and should be designed to maintain system consistency and integrity.
  /// </para>
  /// <para>
  /// Commands with responses typically return the created or modified entity,
  /// or confirmation data about the operation performed.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public record CreateUserCommand(string Email, string FirstName, string LastName) : ICommand&lt;User&gt;;
  /// public record UpdateUserCommand(Guid Id, string FirstName, string LastName) : ICommand&lt;User&gt;;
  /// public record ProcessPaymentCommand(Guid OrderId, PaymentDetails Payment) : ICommand&lt;PaymentResult&gt;;
  /// </code>
  /// </example>
  public interface ICommand<TResponse>: IRequest<TResponse> { }
}
