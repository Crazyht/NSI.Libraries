namespace NSI.Core.Mediator.Abstractions {
  /// <summary>
  /// Marker interface for fire-and-forget notifications that may be handled by multiple handlers.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Notifications represent events that have occurred in the system and may be of interest
  /// to multiple parts of the application. They follow a publish-subscribe pattern where
  /// multiple handlers can react to the same notification.
  /// </para>
  /// <para>
  /// Key characteristics:
  /// <list type="bullet">
  ///   <item><description>Fire-and-forget: The sender doesn't wait for completion</description></item>
  ///   <item><description>Multiple handlers: Zero or more handlers can process the notification</description></item>
  ///   <item><description>No return value: Notifications don't return meaningful data</description></item>
  ///   <item><description>Resilient: Failure of one handler doesn't affect others</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public record UserCreatedNotification(Guid UserId, string Email, DateTime CreatedAt) : INotification;
  /// public record OrderCompletedNotification(Guid OrderId, Guid CustomerId, decimal Amount) : INotification;
  /// public record PaymentFailedNotification(Guid PaymentId, string Reason) : INotification;
  /// 
  /// // Multiple handlers can process the same notification
  /// public class SendWelcomeEmailHandler : IRequestHandler&lt;UserCreatedNotification, Unit&gt; { }
  /// public class CreateUserProfileHandler : IRequestHandler&lt;UserCreatedNotification, Unit&gt; { }
  /// public class UpdateAnalyticsHandler : IRequestHandler&lt;UserCreatedNotification, Unit&gt; { }
  /// </code>
  /// </example>
  public interface INotification: IRequest<Unit> { }
}
